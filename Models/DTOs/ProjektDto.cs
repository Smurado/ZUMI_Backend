namespace ZUMI_Backend.Models.DTOs
{
    public class ProjektDto
    {
        public Guid Id { get; set; }
        public string Kurztitel { get; set; }
        public string Kurzbeschreibung { get; set; }
        public string Beschreibung { get; set; }
        public string Vorbereitungszeitraum { get; set; }
        public string Umsetzungszeitraum { get; set; }
        public string Adresse { get; set; }
        public string Plz { get; set; }
        public Guid ProjektstatusId { get; set; }
 
        public List<Guid> SdgsIds { get; set; } = new List<Guid>();
    }
}