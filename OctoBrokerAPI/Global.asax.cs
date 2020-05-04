using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Octobroker;

namespace OctoBrokerAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            StartOctoprintConnection();
        }

        private static OctoprintConnection octoconnection;
        private void StartOctoprintConnection()
        {
            octoconnection = new OctoprintConnection("http://192.168.1.38:5000", "E3C06441F4834FD2B94E8C75FD3DF915");
        }

        public static OctoprintConnection GetOctoConnection()
        {
            if (octoconnection != null)
                return octoconnection;
            else 
                return octoconnection = new OctoprintConnection("http://192.168.1.38:5000", "E3C06441F4834FD2B94E8C75FD3DF915");
        }
    }
}
