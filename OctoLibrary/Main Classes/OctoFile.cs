using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SlicingBroker;
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
        public event EventHandler<FileSlicedArgs> FileSliced;

        public OctoFile(JObject payload, bool isFileAddedEvent = false)
        {
            //FileReadyForSlicing += OnFileReadyForSlicing;
            //FileSliced += OnFileSliced;
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

        //private void DownloadAndSliceAndUploadAssociatedFile(string location, string DownloadPath, OctoprintConnection Connection)
        //{
        //    JObject info = Connection.Files.GetFileInfo(location, this.Path);
        //    JToken refs = info.Value<JToken>("refs");
        //    string downloadLink = refs.Value<string>("download");
        //    this.Connection = Connection;

        //    using (WebClient myWebClient = new WebClient())
        //    {
        //        try
        //        {
        //            //myWebClient.DownloadFileAsync(new Uri(downloadLink), DownloadPath);
        //            myWebClient.DownloadFile(new Uri(downloadLink), DownloadPath + FileName);
        //            LocalFilePath = DownloadPath + FileName;
        //            SlicedFilePath = DownloadPath + FileName;
        //            SlicedFilePath = System.IO.Path.ChangeExtension(LocalFilePath, null);
        //            FileReadyForSlicing?.Invoke(this, new FileReadyForSlicingArgs(LocalFilePath));
        //            SliceAndUpload(SlicedFilePath);
        //        }
        //        catch (Exception e)
        //        {
        //            var msg = e.Message;
        //        }

        //    }

        //}

        public  void DownloadAssociatedOnlineFile(string location, string downloadPath, OctoprintConnection connection)
        {
            // if the file already exists in the download folder
            if (File.Exists(downloadPath + FileName))
            {
                SetDownloadedFileLocalInformation(downloadPath);
                return;
            }
            

            JObject info = connection.Files.GetFileInfo(location, this.Path);
            JToken refs = info.Value<JToken>("refs");
            string downloadLink = refs.Value<string>("download");

            using (WebClient myWebClient = new WebClient())
            {
                try
                {
                    myWebClient.DownloadFile(new Uri(downloadLink), downloadPath + FileName);
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
            // this.Connection = connection;
            LocalFilePath = downloadPath + FileName;
            SlicedFilePath = downloadPath + FileName;
            SlicedFilePath = System.IO.Path.ChangeExtension(LocalFilePath, null);
            FileReadyForSlicing?.Invoke(this, new FileReadyForSlicingArgs(LocalFilePath));
        }

        //private void SliceAndUpload(string OutputFilePath)
        //{
        //    SliceWithPrusa(new PrusaSlicerBroker(), OutputFilePath);
        //    UploadToOctoprintAsync(SlicedFilePath, Connection);
        //}

        private async Task SliceWithPrusa(PrusaSlicerBroker prusaSlicer, string OutputPath = "")
        {

            // the stl file path to be sliced
            prusaSlicer.FilePath = LocalFilePath;
            //if the path of the output gcode file is specified then slice and put it in that path (must be specified without .gcode extension)
            if (OutputPath != null)
                prusaSlicer.OutputPath = OutputPath;
            // if there is no specific slicing path, slice in the same place of the stl but remove the .stl first of the sliced path then append .gcode 
            else
                OutputPath = System.IO.Path.ChangeExtension(LocalFilePath, null);
            await prusaSlicer.Slice();
            this.SlicedFilePath = OutputPath + ".gcode";
            FileSliced?.Invoke(this, new FileSlicedArgs(SlicedFilePath));
        }

        public async Task Slice(ISlicerBroker slicer, string OutputPath = "")
        {
            PrusaSlicerBroker prusaSlicer = (PrusaSlicerBroker)slicer;
            if (prusaSlicer == null)
                return;
            await SliceWithPrusa(prusaSlicer, OutputPath);
        }

        //public async Task UploadToOctoprintAsync(string SlicedFilePath,OctoprintConnection Connection)
        //{
        //    if (Connection==null)
        //        return;
        //    string uploadResponse = await  Connection.Files.UploadFile(SlicedFilePath, SlicedFileName, "");
        //}


        public async Task<string> UploadToOctoprintAsync(string SlicedFilePath, OctoprintConnection Connection)
        {
            if (Connection == null)
                return "";
            return await Connection.Files.UploadFile(SlicedFilePath, SlicedFileName, "");
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

    public class FileSlicedArgs : EventArgs
    {
        public FileSlicedArgs(string filePath)
        {
            SlicedFilePath = filePath;
        }

        public string SlicedFilePath { get; set; }
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
