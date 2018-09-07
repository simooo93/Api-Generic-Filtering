using LinqKit;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Globalization;
using Microsoft.Extensions.Logging;
using ApiFilteringLibrary.Models;
using ApiFilteringLibrary.Enums;

namespace ApiFilteringLibrary.ExpressionManagers
{
    public class FilterExpressionsManager : IFilterExpressionsManager
    {
        private readonly MethodInfo StringEqualsMethod = typeof(string)
            .GetMethod(
                @"Equals",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null);

        private readonly MethodInfo StringContainsMethod = typeof(string)
            .GetMethod(
                @"Contains",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null);

        private readonly MethodInfo BooleanEqualsMethod = typeof(bool)
            .GetMethod(
                @"Equals",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(bool) },
                null);


        private readonly MethodInfo IntEqualsMethod = typeof(int)
            .GetMethod(
                @"Equals",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(int) },
                null);

        private readonly MethodInfo EnumEqualsMethod = typeof(Enum)
            .GetMethod(
                @"Equals",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Enum) },
                null);

        private readonly MethodInfo DoubleEqualsMethod = typeof(double)
            .GetMethod(
                @"Equals",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(double) },
                null);

        private readonly MethodInfo DecimalEqualsMethod = typeof(decimal)
            .GetMethod(
                @"Equals",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(decimal) },
                null);

        private readonly MethodInfo DateTimeEqualsMethod = typeof(DateTime)
            .GetMethod(
                @"Equals",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(DateTime) },
                null);

        private ILogger<FilterExpressionsManager> _logger;

        public FilterExpressionsManager(ILogger<FilterExpressionsManager> logger)
        {
            this._logger = logger;
        }

        public Expression<Func<Entity, bool>> BuildDbPredicate<Entity, Model>(Filter filter)
        {
            Expression<Func<Entity, bool>> predicate = (x) => true;

            try
            {
                var filteredProperty = typeof(Model).GetProperty(filter.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                // Get the name of the DB field, which may not be the same as the property name.
                var dbFieldName = GetDbFieldName(filteredProperty);
                // Get the target DB type (table)
                var dbType = typeof(Entity);
                // Get a MemberInfo for the type's field (ignoring case
                // so "FirstName" works as well as "firstName")

                this._logger.LogDebug("Filtering for db Member {1} in Model {2}", dbFieldName, typeof(Entity));

                return GetPredicate(filter, ref predicate, filteredProperty, dbFieldName, dbType);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Error in creating predicate in {0}, for Member {1} in Model {2} with Exception {3}", nameof(BuildDbPredicate), filter.Member, typeof(Model), ex.Message);

                return predicate;
            }
        }

        public Expression<Func<Model, bool>> BuildModelPredicate<Model>(Filter filter)
        {
            Expression<Func<Model, bool>> predicate = (x) => true;

            try
            {
                var propertyInfo = typeof(Model).GetProperty(filter.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                {
                    return predicate;
                }

                var fieldMapAttribute = propertyInfo.GetCustomAttributes(typeof(FieldMapAttribute), false).FirstOrDefault() as FieldMapAttribute;

                if (fieldMapAttribute == null || !fieldMapAttribute.IsModelOnlyProperty)
                {
                    return predicate;
                }

                var modelType = typeof(Model);

                this._logger.LogDebug("Filtering for memory Member {1} in Model {2}", filter.Member, modelType);

                return GetPredicate(filter, ref predicate, propertyInfo, propertyInfo.Name, modelType);
            }
            catch (Exception ex)
            {
                this._logger.LogError("Error in creating predicate in {0}, for Member {1} in Model {2} with Exception {3}", nameof(BuildModelPredicate), filter.Member, typeof(Model), ex.Message);

                return predicate;
            }
        }

        private Expression<Func<Entity, bool>> GetPredicate<Entity>(Filter filter, ref Expression<Func<Entity, bool>> predicate, PropertyInfo propertyInfo, string fieldName, Type type)
        {
            try
            {
                var dbFieldMemberInfo = type.GetMember(fieldName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance).Single();

                // STRINGS
                if (propertyInfo.PropertyType == typeof(string))
                {
                    predicate = ApplyStringCriterion(filter.Value,
                     propertyInfo, type, dbFieldMemberInfo, predicate, filter.Operator);
                }
                // BOOLEANS
                else if (propertyInfo.PropertyType == typeof(bool) || propertyInfo.PropertyType == typeof(bool?))
                {
                    predicate = ApplyBoolCriterion(filter.Value,
                      propertyInfo, type, dbFieldMemberInfo, predicate, filter.Operator);
                }
                // INTS...
                else if (propertyInfo.PropertyType == typeof(int) || propertyInfo.PropertyType == typeof(int?))
                {
                    predicate = ApplyIntCriterion(filter.Value,
                      propertyInfo, type, dbFieldMemberInfo, predicate, filter.Operator);
                }
                //DOUBLES
                else if (propertyInfo.PropertyType == typeof(double) || propertyInfo.PropertyType == typeof(double?))
                {
                    predicate = ApplyDoubleCriterion(filter.Value,
                      propertyInfo, type, dbFieldMemberInfo, predicate, filter.Operator);
                }
                //DECIMALS
                else if (propertyInfo.PropertyType == typeof(decimal) || propertyInfo.PropertyType == typeof(decimal?))
                {
                    predicate = ApplyDecimalCriterion(filter.Value,
                      propertyInfo, type, dbFieldMemberInfo, predicate, filter.Operator);
                }
                //DATES
                else if (propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(DateTime?))
                {
                    predicate = ApplyDateTimeCriterion(filter.Value,
                      propertyInfo, type, dbFieldMemberInfo, predicate, filter.Operator);
                }
                //ENUMS
                else if (propertyInfo.PropertyType.IsEnum)
                {
                    predicate = ApplyEnumCriterion(filter.Value,
                      propertyInfo, type, dbFieldMemberInfo, predicate, filter.Operator);
                }

                return predicate;
            }
            catch
            {
                return predicate;
            }
        }

        private Expression<Func<T, bool>> ApplyStringCriterion<T>(string value, PropertyInfo searchCriterionPropertyInfo,
            Type dbType, MemberInfo dbFieldMemberInfo, Expression<Func<T, bool>> predicate, FilterOperator filterOperator)
        {

            // Check if a search criterion was provided
            var searchString = value;
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return predicate;
            }
            // Then "and" it to the predicate.
            // e.g. predicate = predicate.And(x => x.firstName.Contains(searchCriterion.FirstName)); ...
            // Create an "x" as TDbType
            var dbTypeParameter = Expression.Parameter(dbType, @"x");
            // Get at x.firstName
            var dbFieldMember = Expression.MakeMemberAccess(dbTypeParameter, dbFieldMemberInfo);

            // Create the criterion as a constant
            Expression criterionConstant = Expression.Constant(searchString);

            // Create the MethodCallExpression like x.firstName.Contains(criterion)
            Expression methodCall;
            if (filterOperator == FilterOperator.Contains)
            {
                methodCall = Expression.Call(dbFieldMember, StringContainsMethod, criterionConstant);
            }
            else if (filterOperator == FilterOperator.NotContains)
            {
                var containsExpression = Expression.Call(dbFieldMember, StringContainsMethod, criterionConstant);

                methodCall = Expression.Not(containsExpression);
            }
            else if (filterOperator == FilterOperator.NotEqual)
            {
                methodCall = Expression.NotEqual(criterionConstant, dbFieldMember);
            }
            else if (filterOperator == FilterOperator.LessThan)
            {
                methodCall = Expression.LessThan(criterionConstant, dbFieldMember);
            }
            else if (filterOperator == FilterOperator.GreaterThan)
            {
                methodCall = Expression.GreaterThan(criterionConstant, dbFieldMember);
            }
            else
            {
                methodCall = Expression.Call(dbFieldMember, StringEqualsMethod, criterionConstant);
            }

            // Create a lambda like x => x.firstName.Contains(criterion)
            var lambda = Expression.Lambda(methodCall, dbTypeParameter) as Expression<Func<T, bool>>;
            // Apply!

            return predicate.And(lambda);
        }

        private Expression<Func<T, bool>> ApplyBoolCriterion<T>(string value, PropertyInfo searchCriterionPropertyInfo,
          Type dbType, MemberInfo dbFieldMemberInfo, Expression<Func<T, bool>> predicate, FilterOperator filterOperator)
        {
            // Check if a search criterion was provided
            if (!bool.TryParse(value, out bool searchBool))
            {
                return predicate;
            }
            // Then "and" it to the predicate.
            // e.g. predicate = predicate.And(x => x.isActive.Contains(searchCriterion.IsActive)); ...
            // Create an "x" as TDbType
            var dbTypeParameter = Expression.Parameter(dbType, @"x");
            // Get at x.isActive
            var dbFieldMember = Expression.MakeMemberAccess(dbTypeParameter, dbFieldMemberInfo);
            // Create the criterion as a constant
            var criterionConstant = Expression.Constant(searchBool);

            // Create the MethodCallExpression like x.isActive.Equals(criterion)
            Expression exp;

            if (filterOperator == FilterOperator.NotEqual)
            {
                exp = Expression.NotEqual(criterionConstant, dbFieldMember);
            }
            else
            {
                exp = Expression.Call(dbFieldMember, BooleanEqualsMethod, criterionConstant);
            }

            // Create a lambda like x => x.isActive.Equals(criterion)
            var lambda = Expression.Lambda(exp, dbTypeParameter) as Expression<Func<T, bool>>;
            // Apply!
            return predicate.And(lambda);
        }

        private Expression<Func<T, bool>> ApplyIntCriterion<T>(string value, PropertyInfo searchCriterionPropertyInfo,
          Type dbType, MemberInfo dbFieldMemberInfo, Expression<Func<T, bool>> predicate, FilterOperator filterOperator)
        {
            // Check if a search criterion was provided
            if (!int.TryParse(value, out int searchInt))
            {
                return predicate;
            }
            // Then "and" it to the predicate.
            // e.g. predicate = predicate.And(x => x.isActive.Contains(searchCriterion.IsActive)); ...
            // Create an "x" as TDbType
            var dbTypeParameter = Expression.Parameter(dbType, @"x");
            // Get at x.isActive
            var dbFieldMember = Expression.MakeMemberAccess(dbTypeParameter, dbFieldMemberInfo);
            // Create the criterion as a constant
            var criterionConstant = Expression.Constant(searchInt);

            // Create the MethodCallExpression like x.isActive.Equals(criterion)
            Expression exp;

            if (filterOperator == FilterOperator.NotEqual)
            {
                exp = Expression.NotEqual(criterionConstant, dbFieldMember);
            }
            else if (filterOperator == FilterOperator.LessThan)
            {
                exp = Expression.LessThan(dbFieldMember, criterionConstant);
            }
            else if (filterOperator == FilterOperator.GreaterThan)
            {
                exp = Expression.GreaterThan(dbFieldMember, criterionConstant);
            }
            else
            {
                exp = Expression.Equal(dbFieldMember, criterionConstant);
            }

            // Create a lambda like x => x.isActive.Equals(criterion)
            var lambda = Expression.Lambda(exp, dbTypeParameter) as Expression<Func<T, bool>>;
            // Apply!
            return predicate.And(lambda);
        }

        private Expression<Func<T, bool>> ApplyDoubleCriterion<T>(string value, PropertyInfo searchCriterionPropertyInfo,
          Type dbType, MemberInfo dbFieldMemberInfo, Expression<Func<T, bool>> predicate, FilterOperator filterOperator)
        {
            // Check if a search criterion was provided
            if (!double.TryParse(value, out double searchDouble))
            {
                return predicate;
            }

            // Create an "x" as TDbType
            var dbTypeParameter = Expression.Parameter(dbType, @"x");
            // Get at x.isActive
            var dbFieldMember = Expression.MakeMemberAccess(dbTypeParameter, dbFieldMemberInfo);
            // Create the criterion as a constant
            var criterionConstant = Expression.Constant(searchDouble);

            // Create the MethodCallExpression like x.isActive.Equals(criterion)
            Expression exp;

            if (filterOperator == FilterOperator.NotEqual)
            {
                exp = Expression.NotEqual(criterionConstant, dbFieldMember);
            }
            else if (filterOperator == FilterOperator.LessThan)
            {
                exp = Expression.LessThan(dbFieldMember, criterionConstant);
            }
            else if (filterOperator == FilterOperator.GreaterThan)
            {
                exp = Expression.GreaterThan(dbFieldMember, criterionConstant);
            }
            else
            {
                exp = Expression.Call(dbFieldMember, DoubleEqualsMethod, criterionConstant);
            }

            // Create a lambda like x => x.isActive.Equals(criterion)
            var lambda = Expression.Lambda(exp, dbTypeParameter) as Expression<Func<T, bool>>;

            // Apply!
            return predicate.And(lambda);
        }

        private Expression<Func<T, bool>> ApplyDecimalCriterion<T>(string value, PropertyInfo searchCriterionPropertyInfo,
         Type dbType, MemberInfo dbFieldMemberInfo, Expression<Func<T, bool>> predicate, FilterOperator filterOperator)
        {
            // Check if a search criterion was provided
            if (!decimal.TryParse(value, out decimal searchDecimal))
            {
                return predicate;
            }

            // Create an "x" as TDbType
            var dbTypeParameter = Expression.Parameter(dbType, @"x");
            // Get at x.isActive
            var dbFieldMember = Expression.MakeMemberAccess(dbTypeParameter, dbFieldMemberInfo);
            // Create the criterion as a constant
            var criterionConstant = Expression.Constant(searchDecimal);

            // Create the MethodCallExpression like x.isActive.Equals(criterion)
            Expression exp;

            if (filterOperator == FilterOperator.NotEqual)
            {
                exp = Expression.NotEqual(criterionConstant, dbFieldMember);
            }
            else if (filterOperator == FilterOperator.LessThan)
            {
                exp = Expression.LessThan(dbFieldMember, criterionConstant);
            }
            else if (filterOperator == FilterOperator.GreaterThan)
            {
                exp = Expression.GreaterThan(dbFieldMember, criterionConstant);
            }
            else
            {
                exp = Expression.Call(dbFieldMember, DoubleEqualsMethod, criterionConstant);
            }

            // Create a lambda like x => x.isActive.Equals(criterion)
            var lambda = Expression.Lambda(exp, dbTypeParameter) as Expression<Func<T, bool>>;

            // Apply!
            return predicate.And(lambda);
        }


        private Expression<Func<T, bool>> ApplyEnumCriterion<T>(string value, PropertyInfo propertyInfo,
          Type dbType, MemberInfo dbFieldMemberInfo, Expression<Func<T, bool>> predicate, FilterOperator filterOperator)
        {
            // Check if a search criterion was provided

            if (!(Enum.Parse(propertyInfo.PropertyType, value) is Enum enumVal))
            {
                return predicate;
            }

            // Then "and" it to the predicate.
            // e.g. predicate = predicate.And(x => x.isActive.Contains(searchCriterion.IsActive)); ...
            // Create an "x" as TDbType
            var dbTypeParameter = Expression.Parameter(dbType, @"x");
            // Get at x.isActive
            var dbFieldMember = Expression.MakeMemberAccess(dbTypeParameter, dbFieldMemberInfo);
            // Create the criterion as a constant
            var criterionConstant = Expression.Constant(enumVal);

            Expression exp;

            if (filterOperator == FilterOperator.NotEqual)
            {
                exp = Expression.NotEqual(criterionConstant, dbFieldMember);
            }
            else
            {
                exp = Expression.Call(dbFieldMember, EnumEqualsMethod, Expression.Convert(criterionConstant, typeof(object)));
            }

            // Create a lambda like x => x.isActive.Equals(criterion)
            var lambda = Expression.Lambda(exp, dbTypeParameter) as Expression<Func<T, bool>>;
            // Apply!
            return predicate.And(lambda);
        }

        private Expression<Func<T, bool>> ApplyDateTimeCriterion<T>(string value, PropertyInfo propertyInfo,
          Type dbType, MemberInfo dbFieldMemberInfo, Expression<Func<T, bool>> predicate, FilterOperator filterOperator)
        {
            // Check if a search criterion was provided
            DateTime date = new DateTime();
            DateTime nextDay = new DateTime();

            try
            {
                date = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
                nextDay = date.AddDays(1);
            }
            catch
            {
                return predicate;
            }

            // Create an "x" as TDbType
            var dbTypeParameter = Expression.Parameter(dbType, @"x");
            // Get at x.isActive
            var dbFieldMember = Expression.MakeMemberAccess(dbTypeParameter, dbFieldMemberInfo);

            // Create the criterion as a constant
            var dateCriteria = Expression.Constant(date);
            var nextDayCriteria = Expression.Constant(nextDay);


            Expression exp;

            if (filterOperator == FilterOperator.NotEqual)
            {
                exp = Expression.NotEqual(dateCriteria, dbFieldMember);
            }
            else if (filterOperator == FilterOperator.LessThan)
            {
                exp = Expression.LessThan(dbFieldMember, dateCriteria);
            }
            else if (filterOperator == FilterOperator.GreaterThan)
            {
                exp = Expression.GreaterThan(dbFieldMember, dateCriteria);
            }
            else
            {
                var grExp = Expression.GreaterThan(dbFieldMember, dateCriteria);
                var ltExp = Expression.LessThan(dbFieldMember, nextDayCriteria);

                exp = Expression.And(grExp, ltExp);
            }

            // Create a lambda like x => x.isActive.Equals(criterion)
            var lambda = Expression.Lambda(exp, dbTypeParameter) as Expression<Func<T, bool>>;
            // Apply!
            return predicate.And(lambda);
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
