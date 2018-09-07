using System;

namespace ApiFilteringLibrary.Models
{
    public class FieldMapAttribute : Attribute
    {
        public FieldMapAttribute()
        {

        }

        public string Field { get; set; }

        public bool IsModelOnlyProperty { get; set; }

    }
}
