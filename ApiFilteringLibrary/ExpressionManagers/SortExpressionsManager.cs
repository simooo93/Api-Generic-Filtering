using ApiFilteringLibrary.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiFilteringLibrary.ExpressionManagers
{
    public class SortExpressionsManager : ISortExpressionsManager
    {
        public Expression<Func<Entity, dynamic>> BuildDbPredicate<Entity, Model>(Sort sort)
        {
            var propertyInfo = typeof(Model).GetProperty(sort.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            var fieldMapAttribute = propertyInfo.GetCustomAttributes(typeof(FieldMapAttribute), false).FirstOrDefault() as FieldMapAttribute;

            if (fieldMapAttribute == null || fieldMapAttribute.IsModelOnlyProperty)
            {
                return null;
            }

            // Get the name of the DB field, which may not be the same as the property name.
            var dbFieldName = GetDbFieldName(propertyInfo);
            // Get the target DB type (table)
            var dbType = typeof(Entity);

            var p = Expression.Parameter(typeof(Entity));

            return Expression.Lambda<Func<Entity, dynamic>>(Expression.TypeAs(Expression.Property(p, propertyInfo.Name), typeof(object)), p);
        }

        public Expression<Func<Model, dynamic>> BuildModelPredicate<Model>(Sort sort)
        {
            var propertyInfo = typeof(Model).GetProperty(sort.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                return null;
            }

            var fieldMapAttribute = propertyInfo.GetCustomAttributes(typeof(FieldMapAttribute), false).FirstOrDefault() as FieldMapAttribute;

            if (fieldMapAttribute == null || !fieldMapAttribute.IsModelOnlyProperty)
            {
                return null;
            }

            var p = Expression.Parameter(typeof(Model));

            return Expression.Lambda<Func<Model, dynamic>>(Expression.TypeAs(Expression.Property(p, propertyInfo.Name), typeof(object)), p);
        }

        private string GetDbFieldName(PropertyInfo propertyInfo)
        {
            var fieldMapAttribute =
                 propertyInfo.GetCustomAttributes(typeof(FieldMapAttribute), false).FirstOrDefault();
            var dbFieldName = fieldMapAttribute != null ?
                    ((FieldMapAttribute)fieldMapAttribute).Field : propertyInfo.Name;
            return dbFieldName;
        }
    }
}
