using AutoMapper;
using ZUMI_Backend.Models.DTOs;

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
            
            CreateMap<Projekt, ProjektDto>()
                .ForMember(dest => dest.SdgsIds, opt => 
                    opt.MapFrom(src => src.Sdgs.Select(s => s.Id)));
            
            CreateMap<Projektstatus, ProjektstatusDto>();
            CreateMap<Todo, TodoDto>();
            CreateMap<Erklaerbild, ErklaerbildDto>(); 
            CreateMap<SustainableDevelopmentGoal, SustainableDevelopmentGoalDto>();
            CreateMap<Kooperationseinrichtung, KooperationseinrichtungDto>();
            CreateMap<Materialien, MaterialDto>();
            CreateMap<Rolle, RolleDto>();
            
        }
    }
}
