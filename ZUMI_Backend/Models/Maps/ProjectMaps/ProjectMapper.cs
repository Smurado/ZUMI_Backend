namespace ZUMI_Backend.Models.Maps;
using DTOs;
using Enums;


public static class ProjectMapper
{
    public static ProjectDto MapToProjectDto(this Project project)
    {
        if (project == null) return null;

        return new ProjectDto
        {
            Id = project.Id,
            Kurztitel = project.Kurztitel,
            Kurzbeschreibung = project.Kurzbeschreibung,
            Beschreibung = project.Beschreibung,
            Vorbereitungszeitraum = project.Vorbereitungszeitraum,
            Umsetzungszeitraum = project.Umsetzungszeitraum,
            StandortLink = project.StandortLink,
            Adresse = project.Adresse,
            Plz = project.Plz,
            Spendeninformationen = project.Spendeninformationen,
            WeitereInfos = project.WeitereInfos,
            LetztesUpdate = project.LetztesUpdate,
            GesamtBudget = project.GesamtBudget,
            SpentBudget = project.SpentBudget,
            SpendenLink = project.SpendenLink,
            Finance = project.Finance,
            
            // Titelbild-Logik
            TitelBildId = project.Medien.FirstOrDefault(m => m.IsCoverPicture)?.Id,

            // Status Mapping (Enum -> DTO)
            Projektstatus = project.ProjektStatus.MapToProjektstatusDto(),
            
            Rollen = project.Roles?.Select(r => new ProjectRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Permissions = (int)r.Permissions,
                IsSystemRole = r.IsSystemRole
            }).ToList() ?? new List<ProjectRoleDto>(),

            // --- Personen & Rollen Mapping (Das Herzstück) ---
            Personen = project.Personen?.Select(pp => new PersonRoleDto
            {
                PersonId = pp.PersonId,
                
                // Flags
                IsLiked = pp.IsLiked,
                IsOwner = pp.IsOwner,
                
                // Teilnahme: Wer Rollen hat (oder Owner ist), nimmt teil
                IsParticipating = (pp.Roles != null && pp.Roles.Any()) || pp.IsOwner,
                
                Roles = pp.Roles.Select(r => new ProjectRoleDto
                {
                    Id = r.ProjectRole.Id,
                    Name = r.ProjectRole.Name,
                    Permissions = (int)r.ProjectRole.Permissions,
                    IsSystemRole = r.ProjectRole.IsSystemRole
                }).ToList(),
                
                //RoleNames = pp.Roles?.Select(r => r.ProjectRole.Name).ToList() ?? new List<string>(),

                // Personendaten
                FirstName = pp.Person?.FirstName ?? string.Empty,
                LastName = pp.Person?.LastName ?? string.Empty,
                Email = pp.Person?.Email ?? string.Empty,
                Avatar = pp.Person?.Avatar // Falls du Avatar im DTO hast
                
            })
            //.Where(pr => !string.IsNullOrEmpty(pr.FirstName) || !string.IsNullOrEmpty(pr.LastName))
            .ToList() ?? new List<PersonRoleDto>(),

            // --- Unter-Objekte (Delegation an andere Mapper) ---
            // Hinweis: Diese Methoden liegen aktuell noch in DomainToDtoMapper, 
            // sollten idealerweise auch ausgelagert werden.
            
            Sdgs = project.SdgValues?.Select(v => ((Sdg)v).MapToSdgDto()).ToList() ?? new List<SdgDto>(),

            Kooperationseinrichtungen = DomainToDtoMapper.MapToKooperationseinritungDtos(project.Kooperationseinrichtungen),
            
            Materialien = DomainToDtoMapper.MapToMaterialDtos(project.Materialien),
            
            Todos = DomainToDtoMapper.MapToTodoDtos(project.Todos),
            
            Medien = DomainToDtoMapper.MapToMedienDtos(project.Medien),
            
            FundingItems = DomainToDtoMapper.MapToFundingItemDtos(project.FundingItems)
        };
    }

    public static List<ProjectDto> MapToProjectDtos(this IEnumerable<Project> projects)
    {
        return projects?.Select(project => project.MapToProjectDto())
            .Where(dto => dto != null)
            .ToList() ?? new List<ProjectDto>();
    }
    
    // --- Hilfsmethoden für Updates (Create/Update Logik) ---
    
    public static void ApplyCreateFromDto(this Project project, CreateProjectDto dto)
    {
        if (dto == null) return;

        project.Kurztitel = dto.Kurztitel;
        project.Kurzbeschreibung = dto.Kurzbeschreibung;
        project.Beschreibung = dto.Beschreibung;
        project.Vorbereitungszeitraum = dto.Vorbereitungszeitraum;
        project.Umsetzungszeitraum = dto.Umsetzungszeitraum;
        project.StandortLink = dto.StandortLink;
        project.Adresse = dto.Adresse ?? string.Empty;
        project.Plz = dto.Plz;
        project.Spendeninformationen = dto.Spendeninformationen;
        project.WeitereInfos = dto.WeitereInfos;
        project.GesamtBudget = dto.GesamtBudget ?? 0;
        project.SpentBudget = dto.SpentBudget ?? 0;
        project.SpendenLink = dto.SpendenLink ?? string.Empty;
        project.Finance = dto.Finance;
        
        project.LetztesUpdate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        project.ProjektStatus = ProjektStatus.InVorbereitung; // Default
        project.SdgValues = new List<int>();
    }

    public static void ApplyUpdateFromDto(this Project project, UpdateProjectDto dto)
    {
        if (dto == null) return;

        project.Kurztitel = dto.Kurztitel ?? project.Kurztitel;
        project.Kurzbeschreibung = dto.Kurzbeschreibung ?? project.Kurzbeschreibung;
        project.Beschreibung = dto.Beschreibung ?? project.Beschreibung;
        project.Vorbereitungszeitraum = dto.Vorbereitungszeitraum ?? project.Vorbereitungszeitraum;
        project.Umsetzungszeitraum = dto.Umsetzungszeitraum ?? project.Umsetzungszeitraum;
        project.StandortLink = dto.StandortLink ?? project.StandortLink;
        project.Adresse = dto.Adresse ?? project.Adresse;
        project.Plz = dto.Plz ?? project.Plz;
        project.Spendeninformationen = dto.Spendeninformationen ?? project.Spendeninformationen;
        project.WeitereInfos = dto.WeitereInfos ?? project.WeitereInfos;
        project.GesamtBudget = dto.GesamtBudget ?? project.GesamtBudget;
        project.SpentBudget = dto.SpentBudget ?? project.SpentBudget;
        project.SpendenLink = dto.SpendenLink ?? project.SpendenLink;
        project.Finance = dto.Finance ?? project.Finance;
        
        project.LetztesUpdate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (dto.Projektstatus.HasValue)
            project.ProjektStatus = dto.Projektstatus.Value;

        if (dto.SdgValues != null)
        {
            if (project.SdgValues == null) project.SdgValues = new List<int>();
            project.SdgValues.Clear();
            project.SdgValues.AddRange(dto.SdgValues.Distinct().Where(v => v >= 1 && v <= 17));
        }
    }
}