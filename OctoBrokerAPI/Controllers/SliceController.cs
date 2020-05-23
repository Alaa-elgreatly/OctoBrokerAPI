using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Octobroker.Slicing_broker;
using OctoBrokerAPI.Octoprint_Services;

namespace OctoBrokerAPI.Controllers
{
    public class SliceController : ApiController
    {
        // GET: api/Slice
        public HttpResponseMessage Get()
        {
            string message =
               $"To consume the API issue a post request and input the filepath on octoprint in the request body <br /> " +
                $"in the query string you can specify and special slicing parameters you need with this format <br />" +
                $"fill=x, layer=x, support=false/true, outputpath=s, outputname=s <br />" +
                $"If nothing is specified in the query string then default parameters will be used <br />" +
                $"The post request will: <br /> " +
                $"1- Check Octoprint for the requested file <br />" +
                $"2- Download that file if exists on the server <br />" +
                $"3- Slice that downloaded file with given or default paramerters <br />" +
                $"4- Upload the sliced gcode file to Octoprint <br />";

            return new HttpResponseMessage()
            {
                Content = new StringContent(message, Encoding.UTF8, "text/html")
            };
        }

        

        // POST: api/Slice
        public async Task<string> Post([FromBody]string filepath, [FromUri] int fill = 20, [FromUri]double layer = 0.3, [FromUri] bool support = false, [FromUri]string outputpath = "", [FromUri] string outputname = "")
        {
            var octoConnection = WebApiApplication.GetOctoConnection();
            var octofile = OctoPrintFileServices.CreateOctoFile(octoConnection, filepath);
            if (octofile != null)
            {
                string downloadpath = "G:\\Temp\\";
                octofile.DownloadAssociatedOnlineFile("local", downloadpath, octoConnection);

                PrusaSlicerBroker prusaSlicer = new PrusaSlicerBroker(filepath, fill, layer, support, outputpath, outputname);

                octofile.SliceWithPrusa(prusaSlicer, octofile.LocalFilePath, octofile.SlicedFilePath);

                var uploadResponse = await octofile.UploadToOctoprintAsync(octofile.SlicedFilePath, octoConnection);
                if (uploadResponse.Contains("done\":true"))
                {
                    return $"{octofile.SlicedFileName}";
                }
                else
                {
                    var errorResponse = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Problem occured while uploading sliced file to Octoprint");
                    throw new HttpResponseException(errorResponse);
                }
            }

            else
            {
                var errorResponse = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Problem occured while downloading file from Octoprint");
                throw new HttpResponseException(errorResponse);
            }



        }

        // PUT: api/Slice/5
        public void Put(int id, [FromBody]string value)
        {

            throw new HttpResponseException(HttpStatusCode.NotImplemented);
        }

        // DELETE: api/Slice/5
        public void Delete(int id)
        {
            throw new HttpResponseException(HttpStatusCode.NotImplemented);

        }
    }
}
