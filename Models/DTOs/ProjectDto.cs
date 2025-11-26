namespace ZUMI_Backend.Models.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }

        public string Kurztitel { get; set; } = null!;
        public string? Kurzbeschreibung { get; set; }
        public string? Titelbild { get; set; } = null!;
        public string? Beschreibung { get; set; } = null!;
        public string? Vorbereitungszeitraum { get; set; } = null!;
        public string? Umsetzungszeitraum { get; set; } = null!;
        public string? StandortLink { get; set; }
        public string? Adresse { get; set; } = null!;
        public string? Plz { get; set; } = null!;
        public string? Spendeninformationen { get; set; }
        public string? WeitereInfos { get; set; }
        public string? LetztesUpdate { get; set; }
        public double? GesamtBudget { get; set; }
        public double? SpentBudget { get; set; }
        public string? SpendenLink { get; set; } = null!;
        public string? Finance { get; set; }

        // → Vollständige Objekte (keine IDs mehr!)
        public ProjektstatusDto Projektstatus { get; set; } = null!;

        public List<PersonDto> Personen { get; set; } = new();
        public List<SdgDto> Sdgs { get; set; } = new();
        public List<KooperationseinrichtungDto> Kooperationseinrichtungen { get; set; } = new();
        public List<MaterialDto> Materialien { get; set; } = new();
        public List<TodoDto> Todos { get; set; } = new();
        public List<ErklaerbildDto> Erklaerbilder { get; set; } = new();
    }
}