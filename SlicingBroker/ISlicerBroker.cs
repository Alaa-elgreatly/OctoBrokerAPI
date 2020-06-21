using System;
using System.Threading.Tasks;

namespace SlicingBroker
{
    public interface ISlicerBroker
    {
        //string FilePath { get; set; }
        double LayerHeightInMM { get; }
        bool SupportStructureEnabled { get; }
        int FillDensity { get; }
        string SlicerPath { get; }

        Task SliceAsync(string localFilePath, string outputPath="");

    }
}