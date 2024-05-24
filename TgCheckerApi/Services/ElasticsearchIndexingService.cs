using AutoMapper;
using Elasticsearch.Net;
using Microsoft.EntityFrameworkCore;
using Nest;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using TgCheckerApi.Interfaces;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.DTO;

namespace TgCheckerApi.Services
{
    public class ElasticsearchIndexingService : IElasticsearchIndexingService
    {
        private readonly IElasticClient _elasticClient;
        private readonly TgDbContext _dbContext;
        private readonly IMapper _mapper;

        public ElasticsearchIndexingService(IElasticClient elasticClient, TgDbContext dbContext, IMapper mapper)
        {
            _elasticClient = elasticClient;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public List<ChannelElasticDto> ConvertToDto(IEnumerable<Channel> channels)
        {
            return _mapper.Map<List<ChannelElasticDto>>(channels);
        }

        public async Task InitializeIndicesAsync()
        {
            var channels = await _dbContext.Channels.ToListAsync();
            if (!channels.Any())
                return;

            // Convert channels to DTOs
            var channelDtos = ConvertToDto(channels);
            foreach(var i in channelDtos.Select(x => x.Name))
            {
                Console.WriteLine(i);
            }
            
            // Index DTOs instead of entities
            var response = await _elasticClient.IndexManyAsync(channelDtos);
            if (!response.IsValid)
            {
                // Log error or handle it accordingly
                throw new Exception("Failed to index channels: " + response.DebugInformation);
            }
        }

        public async Task RecreateIndexAsync()
        {
            // Delete the index if it exists
            var indexName = "channels"; // Make sure this matches the index you intend to create

            // First, let's check if the index exists and delete it for a clean setup (for development)
            if ((await _elasticClient.Indices.ExistsAsync(indexName)).Exists)
            {
                await _elasticClient.Indices.DeleteAsync(indexName);
            }

            // Now, create the index with proper settings, mappings, and analyzer configurations
            var createIndexResponse = await _elasticClient.Indices.CreateAsync(indexName, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(1)
                        .Analysis(a => a
                            .Tokenizers(t => t
                                .NGram("ngram_tokenizer", n => n
                                    .MinGram(3)
                                    .MaxGram(4)
                                    .TokenChars(TokenChar.Letter)
                                )
                            )
                            .Analyzers(analyzers => analyzers
                                .Custom("default_russian", ca => ca
                                    .Tokenizer("standard")
                                    .Filters("lowercase", "russian_default_stemmer", "russian_morphology")
                                )
                                .Custom("snowball_russian", ca => ca
                                    .Tokenizer("standard")
                                    .Filters("lowercase", "russian_snowball_stemmer", "russian_morphology")
                                )
                                .Custom("ngram_russian", ca => ca
                                    .Tokenizer("ngram_tokenizer")
                                    .Filters("lowercase", "russian_default_stemmer", "russian_morphology")
                                )
                            )
                            .TokenFilters(tf => tf
                                .Stemmer("russian_default_stemmer", st => st
                                    .Language("russian")
                                )
                                .Snowball("russian_snowball_stemmer", st => st
                                    .Language(SnowballLanguage.Russian)
                                )
                            )
                        )
                    )
                    .Map<ChannelElasticDto>(m => m
                        .AutoMap()
                        .Properties(p => p
                            .Text(t => t
                                .Name(n => n.Description)
                                .Fields(ff => ff
                                    .Text(tt => tt
                                        .Name("default_stemmed")
                                        .Analyzer("default_russian")
                                    )
                                    .Text(tt => tt
                                        .Name("snowball_stemmed")
                                        .Analyzer("snowball_russian")
                                    )
                                    .Text(tt => tt
                                        .Name("ngram")
                                        .Analyzer("ngram_russian")
                                    )
                                )
                            )
                        )
                    )
                );

            if (!createIndexResponse.IsValid)
            {
                throw new Exception($"Failed to create index '{indexName}': {createIndexResponse.DebugInformation}");
            }
        }

        public async Task IndexChannelsAsync(List<Channel> channels)
        {
            Console.WriteLine($"BULTASYNC {channels.Count()}");
            var channelDtos = ConvertToDto(channels);
            Console.WriteLine($"BULTASYNC {channelDtos.Count()}");
            var bulkResponse = await _elasticClient.BulkAsync(b => b
                .IndexMany(channelDtos)
                .Refresh(Refresh.WaitFor)  // Ensure the index is refreshed immediately for debugging
            );

            if (!bulkResponse.IsValid)
            {
                Console.WriteLine("Bulk indexing had issues: " + bulkResponse.DebugInformation);
                foreach (var item in bulkResponse.ItemsWithErrors)
                {
                    Console.WriteLine($"Failed to index document {item.Id}: {item.Error}");
                }
                //throw new Exception("Failed to index some or all channels.");
            }
        }
    }
}
