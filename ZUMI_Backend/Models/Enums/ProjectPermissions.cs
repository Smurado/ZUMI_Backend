namespace ZUMI_Backend.Models.Enums;

[Flags]
public enum ProjectPermissions
{
    None = 0,
    
    // --- Basis & Sichtbarkeit ---
    ViewInternalArea = 1,       // Darf interne Bereiche/Updates sehen
    LikeProject = 2,            // (Optional, falls Liker eine Aktion ist)
    Comment = 4,                // Darf kommentieren
    
    // --- Content Management ---
    ManageTodos = 8,
    ManageMaterialien = 16,
    AddMedia = 32,
    DeleteMedia = 64,
    ManageBasis = 128,      // Neu
    ManageLocations = 256,  // Neu
    ManageTime = 512,       // Neu
    ManageStatus = 1024,    // Neu
    
    // --- Verwaltung ---
    ManageBudget = 2048,         // Finanzen einsehen/ändern
    ManageMembers = 4096,        // Leute einladen/kicken
    ManageKooperationseinrichtung = 8192, // Kooperationseinrichtung bearbeiten
    
    // --- Götterkräfte --
    ManageRoles = 1 << 30,         // Rollen erstellen/bearbeiten
    
    // --- Alles ---
    All = int.MaxValue
}
