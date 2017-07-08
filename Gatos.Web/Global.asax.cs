using Gatos.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Gatos.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private CPUMonitor _cpuMonitor;
        private AllocMonitor _allocMonitor;
        private LeakMonitor _leakMonitor;
        private DeadlockMonitor _deadlockMonitor;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            _cpuMonitor = CPUMonitor.Start();
            _allocMonitor = AllocMonitor.Start();
            _leakMonitor = LeakMonitor.Start();
            _deadlockMonitor = DeadlockMonitor.Start();
        }
    }
}
