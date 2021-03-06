using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octobroker.Octo_Events;
using SlicingBroker;

namespace Octobroker

{
    /// <summary>
    /// is the base Class connecting your project to different parts of Octoprint.
    /// </summary>
    public class OctoprintConnection
    {
        /// <summary>
        /// The end point URL like https://192.168.1.2/
        /// </summary>
        public string EndPoint { get; set; }
        /// <summary>
        /// The end point Api Key like "ABCDE12345"
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The Websocket Client
        /// </summary>
        ClientWebSocket WebSocket { get; set; }
        /// <summary>
        /// Defines if the WebsocketClient is listening and the Tread is running
        /// </summary>
        public volatile bool listening;
        /// <summary>
        /// The size of the web socket buffer. Should work just fine, if the Websocket sends more, it will be split in 4096 Byte and reassembled in this class.
        /// </summary>
        public int WebSocketBufferSize = 4096;

        /// <summary>
        /// Gets or sets the position. of the 3D printer, guesses it if necessary from the GCODE
        /// </summary>
        public OctoprintPosTracker Position { get; set; }
        /// <summary>
        /// Gets or sets files in the Folders of the Octoprint Server
        /// </summary>
        public OctoprintFileTracker Files { get; set; }
        /// <summary>
        /// Starts Jobs or reads progress of the Octoprint Server
        /// </summary>
        public OctoprintJobTracker Jobs { get; set; }
        /// <summary>
        /// Reads the Hardware state, Temperatures and other information.
        /// </summary>
        public OctoprintPrinterTracker Printers { get; set; }

        internal ISlicerBroker defaultSlicer { get; set; }

        public void InitializeDefaultSlicer(string SlicerPath)
        {
            defaultSlicer = new PrusaSlicerBroker(SlicerPath);
        }
        private string applicationFolderPath;
        /// <summary>
        /// Holds the path of the local temp folder associated with this connection
        /// </summary>
        public string ApplicationFolderPath
        {
            get => GetApplicationFolderPath();
        }


        private string GetApplicationFolderPath()
        {
            if (string.IsNullOrEmpty(applicationFolderPath))
            {
                //var tempPath = Path.GetTempPath();
                var appPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var directoryInfo = Directory.CreateDirectory(Path.Combine(appPath, "Octobroker\\"));
                applicationFolderPath = directoryInfo.FullName;

            }
            return applicationFolderPath;
        }


        /// <summary>
        /// Creates a <see cref="T:OctoprintClient.OctoprintConnection"/> 
        /// </summary>
        /// <param name="eP">The endpoint Address like "http://192.168.1.2/"</param>
        /// <param name="aK">The Api Key of the User account you want to use. You can get this in the user settings</param>
        public OctoprintConnection(string eP, string aK)
        {
            SetEndPointDirty(eP);
            ApiKey = aK;
            Position = new OctoprintPosTracker(this);
            Files = new OctoprintFileTracker(this);
            Jobs = new OctoprintJobTracker(this);
            Printers = new OctoprintPrinterTracker(this);
            //source = new CancellationTokenSource();
            //token = source.Token;
        }

        private async Task ConnectEndPointAsync()
        {
            var canceltoken = CancellationToken.None;
            WebSocket = new ClientWebSocket();
            await WebSocket.ConnectAsync(
                new Uri("ws://" + EndPoint.Replace("https://", "").Replace("http://", "") + "sockjs/websocket"),
                canceltoken);
        }

        /*

        let socket = new WebSocket("ws://192.168.1.41:5000/sockjs/websocket");
               
               socket.onopen = function(e) {
               alert("[open] Connection established");
               alert("Sending to server");
               socket.send("My name is John");
               };
               
               socket.onmessage = function(event) {
               alert(`[message] Data received from server: ${event.data}`);
               };


         */


        /// <summary>
        /// Sets the end point from dirty input, checks for common faults.
        /// </summary>
        private void SetEndPointDirty(string eP)
        {
            if (eP.EndsWith("/", StringComparison.Ordinal))
            {
                if (eP.StartsWith("http", StringComparison.Ordinal))
                    EndPoint = eP;
                else
                    EndPoint = "http://" + eP;
            }
            else
            {
                if (eP.StartsWith("http", StringComparison.Ordinal))
                    EndPoint = eP + "/";
                else
                    EndPoint = "http://" + eP + "/";
            }
        }

        /// <summary>
        /// Gets the websocketUrl.
        /// </summary>
        /// <returns>The websocket Url.</returns>
        private string GetWebsocketurl()
        {
            string result = EndPoint;

            result = result.Replace("http://", "");
            result = result.Replace("https://", "");
            result = "ws://" + result + "sockjs/websocket";

            return result;
        }

        /// <summary>
        /// A Get request for any String using your Account
        /// </summary>
        /// <returns>The result as a String, doesn't handle Exceptions</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        public string Get(string location)
        {
            string strResponseValue = string.Empty;
            Debug.WriteLine("This was searched:");
            Debug.WriteLine(EndPoint + location + "?apikey=" + ApiKey);
            WebClient wc = new WebClient();
            wc.Headers.Add("X-Api-Key", ApiKey);
            Stream downStream = wc.OpenRead(EndPoint + location);
            using (StreamReader sr = new StreamReader(downStream))
            {
                strResponseValue = sr.ReadToEnd();
            }
            return strResponseValue;
        }

        /// <summary>
        /// Posts a string with the rights of your Account to a given <paramref name="location"/>..
        /// </summary>
        /// <returns>The Result if any exists. Doesn't handle exceptions</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        /// <param name="arguments">The string to post tp the address</param>
        public string PostString(string location, string arguments)
        {
            string strResponseValue = string.Empty;
            Debug.WriteLine("This was searched:");
            Debug.WriteLine(EndPoint + location + "?apikey=" + ApiKey);
            WebClient wc = new WebClient();
            wc.Headers.Add("X-Api-Key", ApiKey);
            strResponseValue = wc.UploadString(EndPoint + location, arguments);
            return strResponseValue;
        }

        /// <summary>
        /// Posts a JSON object as a string, uses JObject from Newtonsoft.Json to a given <paramref name="location"/>.
        /// </summary>
        /// <returns>The Result if any exists. Doesn't handle exceptions</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        /// <param name="arguments">The Newtonsoft Jobject to post tp the address</param>
        public string PostJson(string location, JObject arguments)
        {
            string strResponseValue = string.Empty;
            Debug.WriteLine("This was searched:");
            Debug.WriteLine(EndPoint + location + "?apikey=" + ApiKey);
            String argumentString = string.Empty;
            argumentString = JsonConvert.SerializeObject(arguments);
            //byte[] byteArray = Encoding.UTF8.GetBytes(argumentString);
            HttpWebRequest request = WebRequest.CreateHttp(EndPoint + location);// + "?apikey=" + apiKey);
            request.Method = "POST";
            request.Headers["X-Api-Key"] = ApiKey;
            request.ContentType = "application/json";
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(argumentString);
            }
            HttpWebResponse httpResponse;
            httpResponse = (HttpWebResponse)request.GetResponse();

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
            return strResponseValue;
        }

        /// <summary>
        /// Posts a Delete request to a given <paramref name="location"/>
        /// </summary>
        /// <returns>The Result if any, shouldn't return anything.</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        public string Delete(string location)
        {
            string strResponseValue = string.Empty;
            Debug.WriteLine("This was deleted:");
            Debug.WriteLine(EndPoint + location + "?apikey=" + ApiKey);
            HttpWebRequest request = WebRequest.CreateHttp(EndPoint + location);// + "?apikey=" + apiKey);
            request.Method = "DELETE";
            request.Headers["X-Api-Key"] = ApiKey;
            HttpWebResponse httpResponse;
            httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
            return strResponseValue;
        }

        /// <summary>
        /// Posts a multipart reqest to a given <paramref name="location"/>
        /// </summary>
        /// <returns>The Result if any.</returns>
        /// <param name="packagestring">A packagestring should be generated elsewhere and input here as a String</param>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        public string PostMultipartOld(string packagestring, string location)
        {
            Debug.WriteLine("A Multipart was posted to:");
            Debug.WriteLine(EndPoint + location + "?apikey=" + ApiKey);
            string strResponseValue = String.Empty;
            var webClient = new WebClient();
            string boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
            webClient.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
            webClient.Headers.Add("X-Api-Key", ApiKey);
            packagestring.Replace("{0}", boundary);
            string package = packagestring.Replace("{0}", boundary);

            var nfile = webClient.Encoding.GetBytes(package);


            File.WriteAllBytes(@"G:\Work\testbytes.txt", nfile);

            byte[] resp = webClient.UploadData(EndPoint + location, "POST", nfile);
            return strResponseValue;
        }
        public async Task<string> PostMultipartAsync(string fileData, string fileName, string location, string path = "")
        {
            var httpClient = new HttpClient();
            var headers = httpClient.DefaultRequestHeaders;

            headers.Add("X-Api-Key", ApiKey);
            Uri requestUri = new Uri(EndPoint + location);


            MultipartFormDataContent multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent(fileData), "file", fileName);
            if (path != "") multipartContent.Add(new StringContent(path), "path");
            string responsebody = string.Empty;
            try
            {

                //Send the GET request
                var response = await httpClient.PostAsync(requestUri, multipartContent);

                responsebody = await response.Content.ReadAsStringAsync();
                //strResponseValue = httpResponseBody;
            }
            catch (Exception ex)
            {
                responsebody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
            return responsebody;

        }

        /// <summary>
        /// Stops the Websocket Thread.
        /// </summary>
        public void WebsocketStop()
        {
            listening = false;
        }

        /// <summary>
        /// Starts the Websocket Thread.
        /// </summary>
        public async Task WebsocketStartAsync()
        {
            if (!listening)
            {
                listening = true;
                await ConnectEndPointAsync();
                //Thread syncthread = new Thread(new ThreadStart(WebsocketSync));
                //syncthread.Start();
                await Task.Run(WebsocketSyncAsync);
            }
        }

        private async Task WebsocketSyncAsync()
        {
            var buffer = new byte[8096];
            while (!WebSocket.CloseStatus.HasValue && listening)
            {
                WebSocketReceiveResult received = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, received.Count);
              
                JObject obj = null;
                
                try
                {
                    obj = JObject.Parse(text);
                }
                catch
                {
                    continue;

                    //this is not relevant in our case, but it's important if the case changed, let's just leave it here
                    #region how he handled a object being sent on parts, assembling them and then parsing

                    /*
                        temporarystorage += text;
                        try
                        {
                            obj = JObject.Parse(temporarystorage);
                            temporarystorage = "";
                        }
                        catch
                        {

                        }
                    */

                    #endregion
                }

                JToken events = obj?.Value<JToken>("event");

                if (events == null)
                    continue;

                string eventName = events.Value<string>("type");

                if (string.IsNullOrEmpty(eventName) || eventName != "FileAdded")
                    continue;

                JObject eventpayload = events.Value<JObject>("payload");
                FileAddedEvent fileEvent = new FileAddedEvent(eventName, eventpayload);

                if (fileEvent.OctoFile.Type == "stl")
                {
                    await DownloadSliceAndUploadTheAddedFileAsync(fileEvent);
                }

            }



        }

        private async Task DownloadSliceAndUploadTheAddedFileAsync(FileAddedEvent fileEvent)
        {
            try
            {
                var downloadpath = GetApplicationFolderPath();


                fileEvent.OctoFile.DownloadAssociatedOnlineFile("local", downloadpath, this);

                if (defaultSlicer != null)
                {
                    await defaultSlicer.SliceAsync(fileEvent.OctoFile.LocalFilePath);
                    fileEvent.OctoFile.SetSlicedInPlaceFileInfo();

                    var uploadResponse =
                        await fileEvent.OctoFile.UploadToOctoprintAsync(fileEvent.OctoFile.SlicedFilePath,
                            this);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
