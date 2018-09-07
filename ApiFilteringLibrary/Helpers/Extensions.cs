using ApiFilteringLibrary.Enums;
using ApiFilteringLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace ApiFilteringLibrary.Helpers
{
    public static class Extensions
    {
        public static RequestDataSource ParseQuery(this HttpRequest request)
        {
            var queryDictionary = QueryHelpers.ParseQuery(request.QueryString.Value);
            RequestDataSource requestDataSource = new RequestDataSource();

            foreach (var query in queryDictionary)
            {
                if (query.Key.Equals(QueryType.Sort.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    var sortMembers = query.Value.FirstOrDefault();

                    foreach (var sortMember in sortMembers.Split('|'))
                    {
                        Sort sort = new Sort();
                        bool descending = false;
                        string memberValue = sortMember;
                        if (sortMember.StartsWith("-"))
                        {
                            descending = true;
                            memberValue = sortMember.Substring(1);
                        }

                        sort.Member = memberValue;
                        sort.Type = descending ? SortType.Descending : SortType.Ascending;

                        requestDataSource.Sorts.Add(sort);
                    }
                }
                else if (query.Key.Equals(QueryType.Fields.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    var fields = query.Value.FirstOrDefault();

                    if (!string.IsNullOrEmpty(fields))
                    {
                        requestDataSource.FieldsRaw = fields;

                        foreach (var field in fields.Split(','))
                        {
                            requestDataSource.Fields.Add(new Field() { Member = field.Trim() });
                        }
                    }
                }
                else if (query.Key.Equals(QueryType.Search.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var val in query.Value)
                    {
                        Filter filter = new Filter();
                        FilterOperator filterOperator = FilterOperator.Contains;
                        string memberValue = val;
                        if (val.StartsWith("!"))
                        {
                            filterOperator = FilterOperator.NotContains;
                            memberValue = val.Substring(1);
                        }

                        filter.Member = query.Key;
                        filter.Value = memberValue;
                        filter.Operator = filterOperator;

                        requestDataSource.Filters.Add(filter);
                    }
                }
                else if (query.Key.Equals(QueryType.Skip.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    requestDataSource.SkipRaw = query.Value.FirstOrDefault();
                }
                else if (query.Key.Equals(QueryType.Take.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    requestDataSource.TakeRaw = query.Value.FirstOrDefault();
                }
                else // filter doesn't have a key
                {
                    foreach (var val in query.Value)
                    {
                        Filter filter = new Filter();
                        FilterOperator filterOperator = FilterOperator.Equal;
                        string memberValue = val;
                        if (val.StartsWith("!"))
                        {
                            filterOperator = FilterOperator.NotEqual;
                            memberValue = val.Substring(1);
                        }
                        else if (val.StartsWith(">"))
                        {
                            filterOperator = FilterOperator.GreaterThan;
                            memberValue = val.Substring(1);
                        }
                        else if (val.StartsWith("<"))
                        {
                            filterOperator = FilterOperator.LessThan;
                            memberValue = val.Substring(1);
                        }

                        filter.Member = query.Key;
                        filter.Value = memberValue.ToLower() == "null" ? null : memberValue;
                        filter.Operator = filterOperator;

                        requestDataSource.Filters.Add(filter);
                    }
                }
            }

            return requestDataSource;
        }

        public static ExpandoObject GetFields<TSource>(this TSource source, string fields)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var dataShapedObject = new ExpandoObject();

            if (string.IsNullOrWhiteSpace(fields))
            {
                PropertyInfo[] propertyInfos = typeof(TSource)
                    .GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    object propertyValue = propertyInfo.GetValue(source);

                    ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }

                return dataShapedObject;
            }

            string[] fieldsAfterSplit = fields.Split(',');

            foreach (string field in fieldsAfterSplit)
            {
                string propertyName = field.Trim();

                PropertyInfo propertyInfo = typeof(TSource)
                    .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                {
                    throw new Exception($"Property {propertyName} wasn't found on {typeof(TSource)}");
                }

                object propertyValue = propertyInfo.GetValue(source);

                ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
            }

            return dataShapedObject;
        }
    }
}
