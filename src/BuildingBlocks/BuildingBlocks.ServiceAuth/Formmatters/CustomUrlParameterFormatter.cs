using Refit;
using System.Reflection;

namespace BuildingBlocks.ServiceAuth.Formmatters
{
    public class CustomUrlParameterFormatter : IUrlParameterFormatter
    {
        public string? Format(
            object? value,
            ICustomAttributeProvider attributeProvider,
            Type type)
        {
            if (value is DateOnly dateOnly)
                return dateOnly.ToString("yyyy-MM-dd");

            return value?.ToString();
        }
    }
}
