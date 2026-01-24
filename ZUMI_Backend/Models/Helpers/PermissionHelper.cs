namespace ZUMI_Backend.Models.Helpers;

using Enums;
using ManyToMany;

public class PermissionHelper
{
    /// <summary>
    /// Gets the Combined Permissions of a person in a project.
    /// </summary>
    /// <param name="pp"></param>
    /// <returns></returns>
    public static ProjectPermissions GetCombinedPermissions(ProjektPerson pp)
    {
        if (pp == null) return ProjectPermissions.None;
        if (pp.IsOwner) return ProjectPermissions.All; // Super-Admin

        // Bitweise ODER-Verknüpfung aller Rollen-Berechtigungen
        return pp.Roles
            .Select(r => r.ProjectRole.Permissions)
            .Aggregate(ProjectPermissions.None, (current, next) => current | next);
    }
}