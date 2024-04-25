using AutoMapper;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.DTO;

namespace TgCheckerApi.MapperProfiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Channel, ChannelGetModel>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.ChannelHasTags.Select(cht => cht.TagNavigation.Text)))
            .ForMember(dest => dest.subType, opt => opt.MapFrom(src =>
            src.ChannelHasSubscriptions.Any() ? src.ChannelHasSubscriptions.Max(chs => chs.TypeId) : null))
            .ForMember(dest => dest.SubscriptionExpirationDate, opt => opt.MapFrom(src =>
            src.ChannelHasSubscriptions.Any() ? src.ChannelHasSubscriptions.Max(chs => chs.Expires) : null));

            CreateMap<Comment, CommentUserProfileGetModel>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.ChannelName, opt => opt.MapFrom(src => src.Channel.Name));

            CreateMap<Report, ReportGetModel>()
                .ForMember(dest => dest.ChannelName, opt => opt.MapFrom(src => src.Channel.Name))
                .ForMember(dest => dest.ChannelUrl, opt => opt.MapFrom(src => src.Channel.Url))
                .ForMember(dest => dest.ReporteeName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.UserTelegramChatId, opt => opt.MapFrom(src => src.User.TelegramId))
                .ForMember(dest => dest.ChannelWebUrl, opt => opt.MapFrom(src => $"https://tgsearch.info/Channel/{src.Channel.Id}"));

            CreateMap<Channel, ChannelElasticDto>();
        }
    }
}
