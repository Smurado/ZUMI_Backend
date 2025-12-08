namespace ZUMI_Backend.Models.Enums;

public enum MediaStatus
{
    /// <summary>
    /// Hochgeladen, aber noch nicht verarbeitet (z.B. wartet auf Konvertierung).
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Wird gerade konvertiert (z.B. im Converter-Container).
    /// </summary>
    Converting = 2,

    /// <summary>
    /// Fertig und bereit zum Streamen.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Konvertierung fehlgeschlagen (z.B. ungültiges Format).
    /// </summary>
    Failed = 4
}