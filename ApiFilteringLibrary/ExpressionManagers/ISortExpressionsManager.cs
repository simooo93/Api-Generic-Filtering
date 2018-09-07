using ApiFilteringLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ApiFilteringLibrary.ExpressionManagers
{
    public interface ISortExpressionsManager
    {
        Expression<Func<Entity, dynamic>> BuildDbPredicate<Entity, Model>(Sort sort);

        Expression<Func<Model, dynamic>> BuildModelPredicate<Model>(Sort sort);
    }
}
