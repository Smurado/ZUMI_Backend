using AutoMapper;
using ZUMI_Backend.Models.DTOs;
using ZUMI_Backend.Models.Enums;
using ZUMI_Backend.Extensions;

namespace ZUMI_Backend.Models
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Person, PersonDto>()
                .ForMember(dest => 
                    dest.ProjekteIds, opt => 
                    opt.MapFrom(src => src.Projekte.Select(p => p.PersonId)));

            CreateMap<Project, ProjectDto>();
            
            CreateMap<Projektstatus, ProjektstatusDto>();
            CreateMap<Todo, TodoDto>();
            CreateMap<Erklaerbild, ErklaerbildDto>(); 
            CreateMap<SustainableDevelopmentGoal, SdgDto>();
            CreateMap<Kooperationseinrichtung, KooperationseinrichtungDto>();
            CreateMap<Material, MaterialDto>();
            CreateMap<Rolle, RolleDto>();
            
            // ═════════════════════════════════════════════════════════════════
            // Feedback-Mappings (neu hinzugefügt)
            // ═════════════════════════════════════════════════════════════════

            // Eingehend: CreateFeedbackDto → Feedback
            CreateMap<CreateFeedbackDto, Feedback>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.AffectedComponent, opt => opt.MapFrom(src => src.AffectedComponent))
                // Subject + Message werden automatisch gemappt
                // User + Recipient werden im Endpoint manuell gesetzt (Navigation-Properties)
                ;

            // Ausgehend: Feedback → FeedbackDto (normale User-Ansicht)
            CreateMap<Feedback, FeedbackDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.GetDisplayName()))
                .ForMember(dest => dest.AffectedComponent, opt => opt.MapFrom(src => src.AffectedComponent.GetDisplayName()))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}".Trim()))
                .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.User.Id))
                ;

            // Ausgehend: Feedback → FeedbackDetailDto (Admin-/Detail-Ansicht)
            CreateMap<Feedback, FeedbackDetailDto>()
                .IncludeBase<Feedback, FeedbackDto>() // erbt alles aus FeedbackDto
                .ForMember(dest => dest.RecipientName, 
                    opt => opt.MapFrom(src => src.Recipient != null 
                        ? $"{src.Recipient.FirstName} {src.Recipient.LastName}".Trim() 
                        : null))
                .ForMember(dest => dest.RecipientId, 
                    opt => opt.MapFrom(src => src.Recipient != null 
                        ? src.Recipient.Id 
                        : (Guid?)null))
                .ForMember(dest => dest.ResolvedAt, opt => opt.MapFrom(src => src.ResolvedAt));
            
        }
    }
}
