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
        private static string Ip = "http://192.168.1.35:5000";
        private static string API = "E3C06441F4834FD2B94E8C75FD3DF915";
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
        private static OctoprintConnection StartOctoprintConnection()
        {
             octoconnection = new OctoprintConnection(Ip, API);
             octoconnection.WebsocketStart();
             return octoconnection;
        }

        public static OctoprintConnection GetOctoConnection()
        {
            if (octoconnection != null)
                return octoconnection;
            else 
                return StartOctoprintConnection();
        }
    }
}
