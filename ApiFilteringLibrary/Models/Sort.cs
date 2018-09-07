using ApiFilteringLibrary.Enums;
using System.Linq;
using System.Reflection;

namespace ApiFilteringLibrary.Models
{
    public class Sort
    {
        public string Member { get; set; }

        public SortType Type { get; set; }

        public bool IsValid<T>()
        {
            var propertyInfo = typeof(T).GetProperty(this.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

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
