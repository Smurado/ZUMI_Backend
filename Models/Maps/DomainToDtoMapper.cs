namespace ZUMI_Backend.Models.Maps;
using DTOs;
using Models;

public static class DomainToDtoMapper
{
    public static ProjectDto MapToProjectDto(this Project project)
    {
        return new ProjectDto
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
            
            // Status
            Projektstatus = project.Projektstatus?.MapToProjektstatusDto(),

            // Many-to-Many & One-to-Many Collections
            Personen = project.Personen?.Select(p => p.Person?.MapToPersonDto()).Where(p => p != null).ToList() ?? new List<PersonDto>(),
            Sdgs = project.Sdgs?.Select(s => s.MapToSdgDto()).ToList() ?? new List<SdgDto>(),
            
            Kooperationseinrichtungen = project.Kooperationseinrichtungen
                ?.Select(k => k.MapToKooperationseinrichtungDto())
                .Where(dto => dto != null)
                .ToList() ?? new List<KooperationseinrichtungDto>(),
            
            Materialien = project.Materialien
                ?.Select(m => m.MapToMaterialDto())
                .Where(dto => dto != null)
                .ToList() ?? new List<MaterialDto>(),
            
            Todos = project.Todos?.Select(t => t.MapToTodoDto()).ToList() ?? new List<TodoDto>(),
            
            Erklaerbilder = project.Erklaerbilder?.Select(e => e.MapToErklaerbildDto()).ToList() ?? new List<ErklaerbildDto>()
        };
    }

    public static ProjektstatusDto MapToProjektstatusDto(this Projektstatus status)
        => status == null ? null : new ProjektstatusDto
        {
            Id = status.Id,
            Bezeichnung = status.Bezeichnung,
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
    
    public static PersonDto MapToPersonDto(this Person person)
        => person == null ? null : new PersonDto
        {
            Id = person.Id, 
            Email = person.Email,
            FirstName = person.FirstName,
            LastName = person.LastName,
            Plz = person.Plz,
            Sprache = person.Sprache,
            RolleId = person.RolleId
            
        };

    public static SdgDto MapToSdgDto(this SustainableDevelopmentGoal sdg)
        => sdg == null ? null : new SdgDto
        {
            Id = sdg.Id, 
            Name = sdg.Name
        };

    public static KooperationseinrichtungDto MapToKooperationseinrichtungDto(this Kooperationseinrichtung k)
        => k == null ? null : new KooperationseinrichtungDto
        {
            Id = k.Id, 
            Name = k.Name
        };

    public static TodoDto MapToTodoDto(this Todo todo)
        => todo == null ? null : new TodoDto
        {
            Id = todo.Id, 
            Title = todo.Titel,
            ProjectId = todo.projectid
        };

    public static ErklaerbildDto MapToErklaerbildDto(this Erklaerbild bild)
        => bild == null ? null : new ErklaerbildDto
        {
            Id = bild.Id, 
            Url = bild.url,
            ProjektId = bild.ProjektId,
        };
}

