using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG
{
    class ProcessorSimple : ProcessorBase
    {
        public override void CreateMosaic(Settings settings)
        {
            var tilewidth = 60;
            var tileheight = 44;
            var pixelsPerTile = tilewidth * tileheight;

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Loading Tiles");
            var tiles = new TileSet(settings.FolderStr);
            var tile = tiles.tiles.First();

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Loading Poster");
            var poster = new Poster(settings.SourceImage, settings.size, settings.size, tilewidth, tileheight);
            var tiledata = new int[tiles.tiles.Count * pixelsPerTile];

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Converting Tiles");
            for (var t = 0; t < tiles.tiles.Count; t++)
            {
                for (var p = 0; p < pixelsPerTile; p++)
                {
                    tiledata[t * pixelsPerTile + p] = tiles.tiles[t].pixels[p].AsInt(settings.colorspace);
                }
            }

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Converting Poster");
            var griddata = new int[pixelsPerTile * poster.cols * poster.rows];
            for (var r = 0; r < poster.rows; r++)
            {
                for (var c = 0; c < poster.cols; c++)
                {
                    var data = Helpers.GetArray(poster.GetSubImage(c, r, 1), settings.colorspace);
                    for (var d = 0; d < data.Length; d++)
                    {
                        griddata[(r * poster.cols + c) * data.Length + d] = data[d];
                    }
                }
            }

            //var scorescount = poster.rows * poster.cols;
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Done Converting");
            var scores = CUDA.Run(tiledata, griddata, pixelsPerTile, settings);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Placing Tiles");
            for (var r = 0; r < poster.rows; r++)
            {
                for (var c = 0; c < poster.cols; c++)
                {
                    poster.PlaceImage(new Placement() { row = r, col = c, size = 1, img = tiles.tiles[scores[r * poster.cols + c].Item2] });
                }
            }

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Exporting Poster");
            poster.Export($"output64", settings.outputfolder);
        }
    }
}
