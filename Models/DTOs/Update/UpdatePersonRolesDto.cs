namespace ZUMI_Backend.Models.DTOs;

using System;
using System.Collections.Generic;

public class UpdatePersonRolesDto
{
    // Die Liste referenziert nun die innere Klasse
    public List<PersonRoleUpdateDto> Personen { get; set; } = new();

    /// <summary>
    /// Nested Class: Existiert nur im Kontext von UpdatePersonRolesDto.
    /// Zugriff von außen via: UpdatePersonRolesDto.PersonRoleUpdateDto
    /// </summary>
    public class PersonRoleUpdateDto
    {
        public Guid PersonId { get; set; }  // Immer erforderlich
        public bool IsOwner { get; set; }   // true = Owner werden, false = Rechte entziehen
        public bool RemoveFromProject { get; set; } = false; // Optional: Entfernen
    }
}