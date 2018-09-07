using ApiFilteringLibrary.ExpressionManagers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApiFilteringLibrary.Helpers
{
    public static class Config
    {
        /// <summary>
        /// Adds the core services.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddTransient<IFilterExpressionsManager, FilterExpressionsManager>();
            services.AddTransient<ISortExpressionsManager, SortExpressionsManager>();
            services.AddTransient<IFilterManager, FilterManager>();

            return services;
        }

    }
}
