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

            CreateMap<Comment, CommentUserProfileGetModel>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.ChannelName, opt => opt.MapFrom(src => src.Channel.Name));

            CreateMap<Report, ReportGetModel>()
                .ForMember(dest => dest.ChannelName, opt => opt.MapFrom(src => src.Channel.Name))
                .ForMember(dest => dest.ChannelUrl, opt => opt.MapFrom(src => src.Channel.Url))
                .ForMember(dest => dest.ReporteeName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.ChannelWebUrl, opt => opt.MapFrom(src => $"http://46.39.232.190:8063/Channel/{src.Channel.Id}"));
        }
    }
}
