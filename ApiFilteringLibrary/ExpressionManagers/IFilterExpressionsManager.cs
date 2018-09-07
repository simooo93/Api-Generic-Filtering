using ApiFilteringLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ApiFilteringLibrary.ExpressionManagers
{
    public interface IFilterExpressionsManager
    {
        Expression<Func<Entity, bool>> BuildDbPredicate<Entity, Model>(Filter filter);

        Expression<Func<Model, bool>> BuildModelPredicate<Model>(Filter filter);
    }
}
