namespace ZUMI_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public enum MediaType
{
    /// <summary>
    /// Medientyp 1: Image – Für statische Bilder wie JPG, PNG oder GIF.
    /// </summary>
    [Display(Name = "Image")]
    Image = 1,

    /// <summary>
    /// Medientyp 2: Video – Für Videodateien wie MP4, AVI oder MOV.
    /// </summary>
    [Display(Name = "Video")]
    Video = 2,

    /// <summary>
    /// Medientyp 3: Audio – Für Audiodateien wie MP3, WAV oder Memos (z. B. M4A).
    /// </summary>
    [Display(Name = "Audio")]
    Audio = 3
}