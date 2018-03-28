using Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG
{
    class Settings
    {
        [ArgsMemberSwitch(0)]
        public string SourceImage { get; set; }

        [ArgsMemberSwitch(1)]
        public string outputfolder { get; set; }

        [ArgsMemberSwitch(new string[] { "p" })]
        public ProcessType ProcessorStr { get; set; } = ProcessType.Simple;

        [ArgsMemberSwitch(new string[] { "f" })]
        public string FolderStr { get; set; }

        [ArgsMemberSwitch(new string[] { "fs" })]
        public string FoldersStr { get; set; }

        [ArgsMemberSwitch(new string[] { "fc" })]
        public string FoldersComplexStr { get; set; }

        [ArgsMemberSwitch(new string[] { "cs" })]
        public ColorSpace colorspace { get; set; } = ColorSpace.RGB;

        [ArgsMemberSwitch(new string[] { "s" })]
        public int size { get; set; } = 64;

        [ArgsMemberSwitch(new string[] { "gpu" })]
        public int gpu { get; set; } = 0;

        [ArgsMemberSwitch(new string[] { "d" })]
        public string ditherStr { get; set; }

        public int[] dither { get; set; } = new int[] { 1, 0, 0, 1 };

        public List<TileSet> TileSets { get; set; }

        [ArgsMemberSwitch(new string[] { "w" })]
        public string weightsStr { get; set; } = "1,1,1";

        public float[] weights { get; set; } = new float[] { 1, 1, 1 };

        [ArgsMemberSwitch(new string[] { "t" })]
        public int threads { get; set; } = -1;
    }
}
