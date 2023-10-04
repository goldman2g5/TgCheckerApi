using AutoMapper;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.MapperProfiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Channel, ChannelGetModel>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.ChannelHasTags.Select(cht => cht.TagNavigation.Text)));

            CreateMap<Comment, CommentGetModel>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username)); // Mapping the username
        }
    }
}
