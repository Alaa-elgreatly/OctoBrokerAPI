using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;
using Octobroker;
using Octobroker.Octo_Events;

namespace OctoBrokerAPI.Octoprint_Services
{
    public class OctoPrintFileServices
    {
        public static string GetFileDownloadURL(OctoprintConnection octoConnection, string filepath)
        {
            var filemanager = octoConnection.Files;
            var fileinfo = filemanager.GetFileInfo("local", filepath);
            JToken refs = fileinfo?.Value<JToken>("refs");
            return refs?.Value<string>("download");
        }
        public static OctoFile CreateOctoFile(OctoprintConnection octoConnection, string filepath)
        {
            var filemanager = octoConnection.Files;
            var fileinfo = filemanager.GetFileInfo("local", filepath);
            if (fileinfo == null)
                return null;
            OctoFile octofile = new OctoFile(fileinfo);
            
            return octofile;
        }
        
    }
}