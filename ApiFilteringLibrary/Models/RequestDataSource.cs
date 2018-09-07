using ApiFilteringLibrary.Enums;
using System.Collections.Generic;

namespace ApiFilteringLibrary.Models
{
    public class RequestDataSource
    {
        public List<Filter> Filters { get; set; } = new List<Filter>();

        public List<Sort> Sorts { get; set; } = new List<Sort>();

        public List<Field> Fields { get; set; } = new List<Field>();

        public string FieldsRaw { get; set; }

        public string SkipRaw { get; set; }

        public string TakeRaw { get; set; }

        public int? Skip
        {
            get
            {
                if (!int.TryParse(this.SkipRaw, out int skip))
                {
                    return null;
                }

                return skip;
            }
        }

        public int? Take
        {
            get
            {
                if(!int.TryParse(this.TakeRaw, out int take))
                {
                    return null;
                }

                return take;
            }
        }

        /// <summary>
        /// If there is a operation on filtering or sorting we should not execute skip and take on db level.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool OperationOnModelExists<T>()
        {
            foreach (var sort in this.Sorts)
            {
                if (sort.IsModelOnly<T>())
                {
                    return true;
                }
            }

            foreach (var filter in this.Filters)
            {
                if (filter.IsModelOnly<T>())
                {
                    return true;
                }
            }

            return false;
        }

        public RequestValidationStatus IsValid<T>()
        {
            foreach (var sort in this.Sorts)
            {
                if (!sort.IsValid<T>())
                {
                    return RequestValidationStatus.InvalidSort;
                }
            }

            foreach (var filter in this.Filters)
            {
                if (!filter.IsValid<T>())
                {
                    return RequestValidationStatus.InvalidFilter;
                }
            }

            foreach (var field in this.Fields)
            {
                if (!field.IsValid<T>())
                {
                    return RequestValidationStatus.InvalidFields;
                }
            }

            if (!string.IsNullOrEmpty(this.TakeRaw))
            {
                if(!int.TryParse(this.TakeRaw,out _))
                {
                    return RequestValidationStatus.InvalidTake;
                }
            }

            if (!string.IsNullOrEmpty(this.SkipRaw))
            {
                if (!int.TryParse(this.SkipRaw, out _))
                {
                    return RequestValidationStatus.InvalidSkip;
                }
            }

            return RequestValidationStatus.Valid;
        }
    }
}
