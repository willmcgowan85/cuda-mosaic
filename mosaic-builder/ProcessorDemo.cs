using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace mosaic_builder
{
    class ProcessorDemo : ProcessorBase
    {
        public override void CreateMosaic(Settings settings)
        {
            var tilewidth = 15;
            var tileheight = 11;
            var pixelsPerTile = tilewidth * tileheight;
            settings.weights = GetWeights(settings.weightsStr);
            settings.dither = GetDither(settings.ditherStr);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Loading Tiles");
            var tiles = new TileSet(settings.FolderStr);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Loading Poster");
            var poster = new Poster(settings.SourceImage, settings.size, settings.size, tilewidth, tileheight);
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Poster Size: {poster.cols}x{poster.rows}");
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Total Threads: {(long)poster.cols * poster.rows * tiles.tiles.Count}");

            var tiledata = new int[tiles.tiles.Count * pixelsPerTile];

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Converting Tiles");
            Parallel.ForEach(Enumerable.Range(0, tiles.tiles.Count), t =>
            {
                for (var p = 0; p < pixelsPerTile; p++)
                {
                    tiledata[t * pixelsPerTile + p] = tiles.tiles[t].pixels[p].AsInt(settings.colorspace);
                }
            });

            var groupcount = poster.GetGroupsCount(settings);
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Total Groups: {groupcount}");
            for (var group = 0; group < groupcount; group++)
            {
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Starting Group: {group}");
                var start = DateTime.Now;
                var griddata = poster.GetGridData(group, settings);
                Timer.preprocess += (DateTime.Now - start).TotalMilliseconds;
                List<int> results = null;
                start = DateTime.Now;
                if (settings.gpu == -1)
                {
                    results = CPU.Run(tiledata, griddata, pixelsPerTile, settings);
                }
                else
                {
                    results = CUDA.Run(tiledata, griddata, pixelsPerTile, settings);
                }
                Timer.process += (DateTime.Now - start).TotalMilliseconds;
                start = DateTime.Now;
                poster.SetResults(results, group, settings, tiles, pixelsPerTile);
                Timer.postprocess += (DateTime.Now - start).TotalMilliseconds;
            }

            var filename = $"{Path.GetFileNameWithoutExtension(settings.SourceImage)}_{settings.size}_{settings.colorspace}";
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Exporting {filename}");
            poster.Export(filename, settings.outputfolder);
            Console.WriteLine($"preproc:{Timer.preprocess / 1000}, proc:{Timer.process / 1000}, post:{Timer.postprocess / 1000}, kernel:{Timer.kernel / 1000}");
        }
    }
}
