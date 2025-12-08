using ZUMI_Backend.Extensions;

namespace ZUMI_Backend.Models.Maps;
using DTOs;
using Models;
using Enums;

public static class DomainToDtoMapper
{
    
    public static ProjectDto MapToProjectDto(this Project project)
    => project == null ? null : new ProjectDto
        {
            Id = project.Id,
            Kurztitel = project.Kurztitel,
            Kurzbeschreibung = project.Kurzbeschreibung,
            Titelbild = project.Titelbild,
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
            
            // Status
            Projektstatus = ((ProjektStatus)project.ProjektStatus).MapToProjektstatusDto(),

            // Many-to-Many & One-to-Many Collections
            
            Personen = project.Personen?.Select(pp => new PersonRoleDto
        {
            PersonId = pp.PersonId,
            IsLiked = pp.IsLiked,
            IsOwner = pp.IsOwner,
            IsParticipating = pp.IsParticipating,
            
            //Direkte Mapping der Summary-Felder
            FirstName = pp.Person?.FirstName ?? string.Empty,
            LastName = pp.Person?.LastName ?? string.Empty,
            Email = pp.Person?.Email ?? string.Empty,
        }).Where(pr => !string.IsNullOrEmpty(pr.FirstName) && !string.IsNullOrEmpty(pr.LastName)).ToList() ?? new List<PersonRoleDto>(),
            
            
            //Personen = project.Personen?.Select(p => p.Person?.MapToPersonDto()).Where(p => p != null).ToList() ?? new List<PersonDto>(),
            
            Sdgs = project.SdgValues?.Select(v => ((Sdg)v).MapToSdgDto()).ToList() ?? new List<SdgDto>(),
            
            Kooperationseinrichtungen = project.Kooperationseinrichtungen
                ?.Select(k => k.MapToKooperationseinrichtungDto())
                .Where(dto => dto != null)
                .ToList() ?? new List<KooperationseinrichtungDto>(),
            
            Materialien = project.Materialien
                ?.Select(m => m.MapToMaterialDto())
                .Where(dto => dto != null)
                .ToList() ?? new List<MaterialDto>(),
            
            Todos = project.Todos?.Select(t => t.MapToTodoDto()).ToList() ?? new List<TodoDto>(),
            
            Medien = project.Medien?.Select(e => e.MapToMedienDto()).ToList() ?? new List<MedienDto>()
        };
    
    public static AltersgruppeDto MapToAltersgruppeDto(this Altersgruppe ag)
    {
        if (ag == null) return null;  // Enum ist nie null, aber für Konsistenz

        return new AltersgruppeDto
        {
            Id = (int)ag,  
            Name = ag.GetDisplayName() 
        };
    }
    
    public static MedienDto MapToMedienDto(this Medien medium)
    {
        if (medium == null) return null;
        return new MedienDto
        {
            Id = medium.Id,
            Url = medium.Url,
            ProjektId = medium.ProjektId,
            Status =  medium.Status,
            MediaType =  medium.MediaType,
            
        };
    }

// Für Listen (z. B. in ProjectDto)
    public static List<MedienDto> MapToMedienDtos(this IEnumerable<Medien> bilder)
    {
        return bilder?.Select(b => b.MapToMedienDto())
            .Where(dto => dto != null)
            .ToList() ?? new List<MedienDto>();
    }
    
    public static MediaTypeDto MapToMediaTypeDto(this MediaType type)
        => type == null ? null : new MediaTypeDto  // Annahme: MediaTypeDto existiert oder erstelle es
        {
            Id = (int)type,
            Name = type.GetDisplayName()  // z.B. "Video"
        };

    public static MediaStatusDto MapToMediaStatusDto(this MediaStatus status)
        => status == null ? null : new MediaStatusDto
        {
            Id = (int)status,
            Name = status.GetDisplayName()  // z.B. "Completed"
        };
    
    public static void ApplyCreateFromDto(this Project project, CreateProjectDto dto)
    {
        if (dto == null) return;

        project.Kurztitel = dto.Kurztitel;
        project.Kurzbeschreibung = dto.Kurzbeschreibung;
        project.Titelbild = dto.Titelbild ?? string.Empty;
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
        project.LetztesUpdate = DateTime.UtcNow.ToString("yyyy-MM-dd");  // Auto-Timestamp

        project.SdgValues = new List<int>();  // Leer beim Create
        // Collections bleiben leer (z. B. Personen via Owner-Add)
    }
    
    // Apply Update for Project
    public static void ApplyUpdateFromDto(this Project project, UpdateProjectDto dto)
    {
        if (dto == null) return;

        // Basis-Felder updaten
        project.Kurztitel = dto.Kurztitel ?? project.Kurztitel;
        project.Kurzbeschreibung = dto.Kurzbeschreibung ?? project.Kurzbeschreibung;
        project.Titelbild = dto.Titelbild ?? project.Titelbild;
        project.Beschreibung = dto.Beschreibung ?? project.Beschreibung;
        project.Vorbereitungszeitraum = dto.Vorbereitungszeitraum ?? project.Vorbereitungszeitraum;
        project.Umsetzungszeitraum = dto.Umsetzungszeitraum ?? project.Umsetzungszeitraum;
        project.StandortLink = dto.StandortLink ?? project.StandortLink;
        project.Adresse = dto.Adresse ?? project.Adresse;
        project.Plz = dto.Plz ?? project.Plz;
        project.Spendeninformationen = dto.Spendeninformationen ?? project.Spendeninformationen;
        project.WeitereInfos = dto.WeitereInfos ?? project.WeitereInfos;
        
        // Immer das aktuelle Datum setzen bei einem Update
        project.LetztesUpdate = DateTime.UtcNow.ToString("yyyy-MM-dd"); 
        
        project.GesamtBudget = dto.GesamtBudget ?? project.GesamtBudget;
        project.SpentBudget = dto.SpentBudget ?? project.SpentBudget;
        project.SpendenLink = dto.SpendenLink ?? project.SpendenLink;
        project.Finance = dto.Finance ?? project.Finance;
        project.ProjektStatus = dto.Projektstatus ?? project.ProjektStatus;

        // SDG-Update: Sicherer machen
        if (dto.SdgValues != null)
        {
            if (project.SdgValues == null) project.SdgValues = new List<int>();
            
            project.SdgValues.Clear();
            project.SdgValues.AddRange(dto.SdgValues.Distinct().Where(v => v >= 1 && v <= 17));
        }
    }
    
    public static List<ProjectDto> MapToProjectDtos(this IEnumerable<Project> projects)
    {
        return projects?.Select(project => project.MapToProjectDto())
            .Where(dto => dto != null)  // Null-DTOs filtern (sicherheitshalber)
            .ToList() ?? new List<ProjectDto>();
    }

    public static ProjektstatusDto MapToProjektstatusDto(this ProjektStatus status) 
    => status == null ? null : new ProjektstatusDto
    {
            Id = (int)status,
            Bezeichnung = status.GetDisplayName(),
    };
    
    public static TodoStatusDto MapToTodoStatusDto(this TodoStatus status)
    => status == null ? null : new TodoStatusDto
    {
        Id = (int)status,
        Bezeichnung = status.GetDisplayName(),
    };
    
    public static MaterialDto MapToMaterialDto(this Material material)
    {
        if (material == null) return null;
        return new MaterialDto
        {
            Id = material.Id,
            Name = material.Name,
            Beschreibung = material.Beschreibung,
            Vorhanden = material.Vorhanden,
        };
    }
    
    public static List<MaterialDto> MapToMaterialDtos(this IEnumerable<Material> materials)
    {
        return materials?.Select(material => material.MapToMaterialDto())
            .Where(dto => dto != null)  // Null-DTOs filtern (sicherheitshalber)
            .ToList() ?? new List<MaterialDto>();
    }
    
    public static PersonDto MapToPersonDto(this Person person)
    => person == null ? null : new PersonDto
        {
            Id = person.Id, 
            Email = person.Email,
            FirstName = person.FirstName,
            LastName = person.LastName,
            Plz = person.Plz,
            Sprache = person.Sprache,
            Altersgruppe = person.Altersgruppe,
        };

    public static SdgDto MapToSdgDto(this Sdg sdg)
    => sdg == null ? null : new SdgDto
    {
        Id = (int)sdg, 
        Name = sdg.GetDisplayName()
    };

    public static KooperationseinrichtungDto MapToKooperationseinrichtungDto(this Kooperationseinrichtung k)
    => k == null ? null : new KooperationseinrichtungDto
    {
        Id = k.Id, 
        Name = k.Name,
        Email = k.Email,
        SocialMedia =  k.SocialMedia,
        Telefonnummer = k.Telefonnummer,
        Website = k.Webseite
    };

    public static List<KooperationseinrichtungDto> MapToKooperationseinritungDtos(
        this IEnumerable<Kooperationseinrichtung> ks)
    {
        return ks?.Select(k => k.MapToKooperationseinrichtungDto())
            .Where(dto => dto != null)
            .ToList() ?? new List<KooperationseinrichtungDto>();
    }

    public static TodoDto MapToTodoDto(this Todo todo)
    => todo == null ? null : new TodoDto
    {
        Id = todo.Id, 
        Title = todo.Titel,
        ProjectId = todo.ProjectId,
        Beschreibung = todo.Beschreibung,
        Status = todo.Status
    };

    public static List<TodoDto> MapToTodoDtos(this IEnumerable<Todo> todos)
    {
        return todos?.Select(todo => todo.MapToTodoDto())
            .Where(dto => dto != null)
            .ToList() ?? new List<TodoDto>();
    }

    public static FeedbackDto MapToFeedbackDto(this Feedback feedback)
    {
        if (feedback == null) return null;

        return new FeedbackDto()
        {
            Id = feedback.Id,
            Category = feedback.Category.GetDisplayName(),
            AffectedComponent = feedback.AffectedComponent.GetDisplayName(),
            Subject = feedback.Subject,
            Message = feedback.Message,
            CreatedAt = feedback.CreatedAt,

            // Null-Check for the GUID
            SenderId = feedback.User?.Id ?? Guid.Empty
        };
    }

    public static List<FeedbackDto> MapToFeedbackDtos(this IEnumerable<Feedback> feedbacks)
    {
        return feedbacks?.Select(f => f.MapToFeedbackDto())
            .Where(dto => dto != null)
            .ToList() ?? new List<FeedbackDto>();
    }
    
    public static FeedbackDetailDto MapToFeedbackDetailDto(this Feedback feedback)
    {
        if (feedback == null) return null;
        
        return new FeedbackDetailDto
        {
            // -- Basis-Felder (aus FeedbackDto) --
            Id = feedback.Id,
            Category = feedback.Category.GetDisplayName(),
            AffectedComponent = feedback.AffectedComponent.GetDisplayName(),
            Subject = feedback.Subject,
            Message = feedback.Message,
            CreatedAt = feedback.CreatedAt,
            SenderId = feedback.User?.Id ?? Guid.Empty,

            // -- Detail-Felder (aus FeedbackDetailDto) --
            SenderEmail = feedback.User?.Email, // Annahme: Person hat Email
            ResolvedAt = feedback.ResolvedAt,
            RecipientId = feedback.Recipient?.Id,
        };
        
        
    }
    
    public static List<FeedbackDetailDto> MapToFeedbackDetailDtos(this IEnumerable<Feedback> feedbacks)
    {
        return feedbacks?.Select(f => f.MapToFeedbackDetailDto())
            .Where(dto => dto != null)
            .ToList() ?? new List<FeedbackDetailDto>();
    }
    
    public static Feedback MapToEntity(this CreateFeedbackDto dto, Person? sender, Person? recipient)
    {
        return new Feedback
        {
            // Da im DTO jetzt echte Enums sind, kein Parsing mehr nötig:
            Category = dto.Category,
            AffectedComponent = dto.AffectedComponent,
                
            Subject = dto.Subject,
            Message = dto.Message,
                
            // Relationen setzen
            User = sender,
            Recipient = recipient,
                
            // Standards setzen
            CreatedAt = DateTimeOffset.UtcNow,
            IsRead = false,
            IsResolved = false
        };
    }
}

