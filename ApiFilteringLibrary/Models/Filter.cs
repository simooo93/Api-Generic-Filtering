using System.Linq;
using System.Reflection;
using ApiFilteringLibrary.Enums;

namespace ApiFilteringLibrary.Models
{
    public class Filter
    {
        public FilterOperator Operator { get; set; }

        public string Member { get; set; }

        public string Value { get; set; }

        public bool IsValid<T>()
        {
            var propertyInfo = typeof(T).GetProperty(this.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if(propertyInfo.PropertyType == typeof(bool) || propertyInfo.PropertyType.IsEnum)
            {
                if(this.Operator == FilterOperator.LessThan || this.Operator == FilterOperator.GreaterThan)
                {
                    return false;
                }
            }

            return propertyInfo != null;
        }

        public bool IsModelOnly<T>()
        {
            var propertyInfo = typeof(T).GetProperty(this.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            var fieldMapAttribute = propertyInfo?.GetCustomAttributes(typeof(FieldMapAttribute), false).FirstOrDefault();

            return fieldMapAttribute != null && ((FieldMapAttribute)fieldMapAttribute).IsModelOnlyProperty;
        }
    }
}
