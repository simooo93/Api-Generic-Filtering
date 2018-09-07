using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqKit;
using ApiFilteringLibrary.Models;
using ApiFilteringLibrary.Enums;
using ApiFilteringLibrary.ExpressionManagers;

namespace ApiFilteringLibrary
{
    public class FilterManager : IFilterManager
    {
        private IFilterExpressionsManager _filterExpressionsManager;
        private ISortExpressionsManager _sortExpressionsManager;

        public FilterManager(IFilterExpressionsManager filterExpressionsManager, ISortExpressionsManager sortExpressionsManager)
        {
            this._filterExpressionsManager = filterExpressionsManager;
            this._sortExpressionsManager = sortExpressionsManager;
        }

        public IQueryable<T> FilterDbCollection<T, Model>(IQueryable<T> collection, RequestDataSource requestSource)
        {
            if (requestSource == null || (requestSource.Filters.Count == 0 && requestSource.Sorts.Count == 0))
            {
                return collection;
            }

            var groupByMembers = requestSource.Filters.GroupBy(x => x.Member);

            List<T> filtered = new List<T>();

            Expression<Func<T, bool>> filterExp = (x) => true;

            foreach (var group in groupByMembers)
            {
                Expression<Func<T, bool>> memberExpression = (x) => false;

                foreach (var filter in group)
                {
                    memberExpression = memberExpression.Or(this._filterExpressionsManager.BuildDbPredicate<T, Model>(filter));
                }

                filterExp = filterExp.And(memberExpression);
            }

            collection = collection.Where(filterExp);

            foreach (var sort in requestSource.Sorts)
            {
                var sortExp = this._sortExpressionsManager.BuildDbPredicate<T, Model>(sort);
                
                if(sortExp == null)
                {
                    continue;
                }
                
                if (sort.Type == SortType.Ascending)
                {
                    collection = collection.OrderBy(sortExp);
                }
                else
                {
                    collection = collection.OrderByDescending(sortExp);
                }
            }

            if (!requestSource.OperationOnModelExists<Model>())
            {
                if (requestSource.Skip.HasValue)
                {
                    collection = collection.Skip(requestSource.Skip.Value);
                }

                if (requestSource.Take.HasValue)
                {
                    collection = collection.Take(requestSource.Take.Value);
                }
            }

            return collection;
        }

        public IEnumerable<T> FilterMemoryCollection<T>(IEnumerable<T> collection, RequestDataSource requestSource)
        {
            if (requestSource == null || (requestSource.Filters.Count == 0 && requestSource.Sorts.Count == 0))
            {
                return collection;
            }

            var groupByMembers = requestSource.Filters.GroupBy(x => x.Member);

            Expression<Func<T, bool>> filterExp = (x) => true;

            foreach (var group in groupByMembers)
            {
                Expression<Func<T, bool>> memberExpression = (x) => false;

                foreach (var filter in group)
                {
                    var expression = this._filterExpressionsManager.BuildModelPredicate<T>(filter);
                    memberExpression = memberExpression.Or(expression);
                }

                filterExp = filterExp.And(memberExpression);
            }

            collection = collection.Where(filterExp.Compile());

            foreach (var sort in requestSource.Sorts)
            {
                var sortExp = this._sortExpressionsManager.BuildModelPredicate<T>(sort);

                if (sortExp == null)
                {
                    continue;
                }

                if (sort.Type == SortType.Ascending)
                {
                    collection = collection.OrderBy(sortExp.Compile());
                }
                else
                {
                    collection = collection.OrderByDescending(sortExp.Compile());
                }
            }

            if (requestSource.OperationOnModelExists<T>())
            {
                if (requestSource.Skip.HasValue)
                {
                    collection = collection.Skip(requestSource.Skip.Value);
                }

                if (requestSource.Take.HasValue)
                {
                    collection = collection.Take(requestSource.Take.Value);
                }
            }

            return collection;
        }
    }
}
