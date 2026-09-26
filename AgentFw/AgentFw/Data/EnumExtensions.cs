using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AgentFw.Data
{
    public static class EnumExtensions
    {
        /// <summary>Returns the [Display(Name = ...)] text of an enum value, or its name.</summary>
        public static string GetDisplayName(this Enum value) =>
            value.GetType()
                 .GetField(value.ToString())?
                 .GetCustomAttribute<DisplayAttribute>()?
                 .GetName() ?? value.ToString();
    }
}
