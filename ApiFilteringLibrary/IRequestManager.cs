using ApiFilteringLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ApiFilteringLibrary
{
    public interface IFilterManager
    {
        IQueryable<T> FilterDbCollection<T, Model>(IQueryable<T> collection, RequestDataSource requestSource);

        IEnumerable<T> FilterMemoryCollection<T>(IEnumerable<T> collection, RequestDataSource requestSource);
    }
}
