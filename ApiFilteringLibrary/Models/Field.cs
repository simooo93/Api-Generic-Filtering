
namespace ApiFilteringLibrary.Models
{
    using System.Reflection;
    
    public class Field
    {
        public string Member { get; set; }

        public bool IsValid<T>()
        {
            var propertyInfo = typeof(T).GetProperty(this.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            return propertyInfo != null;
        }
    }
}
