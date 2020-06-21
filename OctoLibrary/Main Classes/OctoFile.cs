using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SlicingBroker;

namespace Octobroker.Octo_Events
{
    public class OctoFile
    {
       // private OctoprintConnection Connection;
        public string FileName { get; private set; }
        public string Path { get; private set; }
        public string LocalFilePath { get; private set; }
        public string SlicedFilePath { get; set; }
        public string SlicedFileName => System.IO.Path.ChangeExtension(FileName, "gcode");
        public string Type { get; private set; }
        public bool IsFileAddedEvent { get; set; }
        public event EventHandler<FileReadyForSlicingArgs> FileReadyForSlicing;

        public OctoFile(JObject payload, bool isFileAddedEvent = false)
        {
            //FileReadyForSlicing += OnFileReadyForSlicing;
            this.IsFileAddedEvent = isFileAddedEvent;
            ParsePayload(payload);
        }

        public void ParsePayload(JObject Payload)
        {
            FileName = Payload.Value<String>("name") ?? "";
            Path = Payload.Value<String>("path") ?? "";

            if (IsFileAddedEvent)
            {
                var typeArray = Payload.Value<JArray>("type");

                if (typeArray.Any(element => element?.Value<string>() == "gcode"))
                    Type = "gcode";
                if (typeArray.Any(element => element?.Value<string>() == "stl"))
                    Type = "stl";
            }
            else
            {
                var typeArrayPath = Payload.Value<JArray>("typePath");

                if (typeArrayPath.Any(element => element?.Value<string>() == "gcode"))
                    Type = "gcode";
                if (typeArrayPath.Any(element => element?.Value<string>() == "stl"))
                    Type = "stl";
            }

        }



        public OctoprintFile ConvertToOctoprintFile()
        {
            return new OctoprintFile() { Name = FileName, Path = this.Path, Type = this.Type };
        }

        public  void DownloadAssociatedOnlineFile(string location, string downloadPath, OctoprintConnection connection)
        {
            var currentFilePath = System.IO.Path.Combine(downloadPath , FileName);
            // if the file already exists in the download folder then just set its information
            if (File.Exists(currentFilePath))
            {
                SetDownloadedFileLocalInformation(downloadPath);
                return;
            }
            // else download the file from Octoprint then set its information

            JObject info = connection.Files.GetFileInfo(location, this.Path);
            JToken refs = info.Value<JToken>("refs");
            string downloadLink = refs.Value<string>("download");

            using (WebClient myWebClient = new WebClient())
            {
                try
                {
                    myWebClient.DownloadFile(new Uri(downloadLink), currentFilePath);
                    SetDownloadedFileLocalInformation(downloadPath);
                }
                catch (Exception e)
                {
                    var msg = e.Message;
                }

            }
        }

        private void SetDownloadedFileLocalInformation(string downloadPath)
        {
            LocalFilePath = System.IO.Path.Combine(downloadPath, FileName);
            FileReadyForSlicing?.Invoke(this, new FileReadyForSlicingArgs(LocalFilePath));
        }
        /// <summary>
        /// This function should be called right after the associated Octofile has been sliced to add the sliced file path to the octofile
        /// </summary>
        public void SetSlicedInPlaceFileInfo()
        {
            SlicedFilePath = System.IO.Path.ChangeExtension(LocalFilePath, ".gcode");
        }

        public async Task<string> UploadToOctoprintAsync(string SlicedFilePath, OctoprintConnection Connection)
        {
            if (Connection == null)
                return "";
            return await Connection.Files.UploadFileAsync(SlicedFilePath, SlicedFileName, "");
        }

        private void OnFileReadyForSlicing(object sender, EventArgs args)
        {
            var fileInfo = (OctoFile)sender;
            var fileargs = (FileReadyForSlicingArgs)args;
            if (fileargs == null || fileInfo == null)
                return;
            if (string.IsNullOrEmpty(fileargs.FilePath))
                return;
            //SliceAndUpload(fileargs.FilePath,);
        }
        private void OnFileSliced(object sender, EventArgs args)
        {
            var fileInfo = (OctoFile)sender;
            var slicedArgs = (FileSlicedArgs)args;
            if (slicedArgs == null || fileInfo == null)
                return;
            if (string.IsNullOrEmpty(slicedArgs.SlicedFilePath))
                return;

           // UploadToOctoprintAsync(slicedArgs.SlicedFilePath, Connection);
        }

    }



    public class FileReadyForSlicingArgs : EventArgs
    {
        public FileReadyForSlicingArgs(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; set; }

    }
}
