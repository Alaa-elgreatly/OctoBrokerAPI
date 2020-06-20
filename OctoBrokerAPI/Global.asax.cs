using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
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
        private static string ip = ConfigurationManager.AppSettings["OctoprintIP"];
        private static string api = ConfigurationManager.AppSettings["OctoprintAPI"];
        private static string prusaSlicerPath = ConfigurationManager.AppSettings["LocalPrusaSlicerPath"];

        protected async void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            await StartOctoprintConnection();
        }

        private static OctoprintConnection octoconnection;
        private static async Task StartOctoprintConnection()
        {
             octoconnection = new OctoprintConnection(ip, api);
             octoconnection.InitializeDefaultSlicer(prusaSlicerPath);
             await octoconnection.WebsocketStart();
        }

        public static async  Task<OctoprintConnection> GetOctoConnection()
        {
            if (octoconnection != null)
                return octoconnection;
            else
            {
                await StartOctoprintConnection();
                return octoconnection;
            }
                
        }
    }
}
