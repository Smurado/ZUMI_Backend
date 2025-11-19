using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ZUMI_Backend.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        if (enumValue == null) return string.Empty;
        
        var member = enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault();

        var display = member?.GetCustomAttribute<DisplayAttribute>();

        return display?.Name ?? enumValue.ToString();
    }
}