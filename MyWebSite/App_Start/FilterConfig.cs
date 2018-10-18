using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyWebSite
{
    public class FilterConfig
    {
        internal static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // Add the filter to redirect to Views/Shared/Error.cshmtl on 500s errors
            filters.Add(new HandleErrorAttribute());
        }
    }
}