namespace ZUMI_Backend.Models.Helpers;

using Models;
using Models.Enums;

public static class ProjectRoleFactory
{
    public static List<ProjectRole> CreateDefaultRoles(Guid projectId, RoleTemplateType template)
    {
        var roles = new List<ProjectRole>();

        // 1. SYSTEM-ROLLEN (Gibt es IMMER)

        // Mitglied: Basis-Mitglied
        roles.Add(new ProjectRole
        {
            ProjectId = projectId,
            Name = "Mitglied",
            Permissions = ProjectPermissions.ViewInternalArea |
                          ProjectPermissions.Comment,
            IsSystemRole = true
        });
        
        roles.Add(new ProjectRole
        {
            ProjectId = projectId,
            Name = "Projekt-Flüsterer",
            Permissions = ProjectPermissions.ViewInternalArea |
                          ProjectPermissions.Comment |
                          ProjectPermissions.ManageKooperationseinrichtung,
            IsSystemRole = true
        });

        // 2. TEMPLATE-ROLLEN (Je nach Auswahl)
        switch (template)
        {
            case RoleTemplateType.Schulklasse:
                roles.Add(new ProjectRole
                {
                    ProjectId = projectId,
                    Name = "Lehrer",
                    // Lehrer darf fast alles
                    Permissions = ProjectPermissions.All, 
                    IsSystemRole = false
                });
                
                roles.Add(new ProjectRole
                {
                    ProjectId = projectId,
                    Name = "Schüler",
                    // Schüler darf Todos abhaken
                    Permissions = ProjectPermissions.ViewInternalArea |
                                  ProjectPermissions.ManageTodos |
                                  ProjectPermissions.Comment,
                    IsSystemRole = false
                });
                break;

            case RoleTemplateType.Verein:
                roles.Add(new ProjectRole
                {
                    ProjectId = projectId,
                    Name = "Vorstand",
                    Permissions = ProjectPermissions.All,
                    IsSystemRole = false
                });
                 roles.Add(new ProjectRole
                {
                    ProjectId = projectId,
                    Name = "Kassenwart",
                    Permissions = ProjectPermissions.ViewInternalArea | 
                                  ProjectPermissions.ManageBudget,
                    IsSystemRole = false
                });
                break;

            // Standard Case macht nichts weiter
        }

        return roles;
    }
}
