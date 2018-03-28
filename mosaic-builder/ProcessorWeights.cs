using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG
{
    class ProcessorWeights : ProcessorBase
    {
        public override void CreateMosaic(Settings settings)
        {
            var tilewidth = 60;
            var tileheight = 44;
            var pixelsPerTile = tilewidth * tileheight;

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Loading Tiles");
            var tiles = new TileSet(settings.FolderStr);
            //var tile = tiles.tiles.First();

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
            var results = CUDA.RunAdvanced(tiledata, griddata, pixelsPerTile, settings);

            for (var pow = settings.dither[1]; pow <= settings.dither[2]; pow += settings.dither[3])
            {
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Placing Tiles");
                var rng = new Random();
                for (var r = 0; r < poster.rows; r++)
                {
                    for (var c = 0; c < poster.cols; c++)
                    {
                        var starttile = (r * poster.cols + c) * settings.dither[0];
                        var tile = starttile;
                        if (pow > 0)
                        {
                            var maxrand = 0.0d;
                            for (var t = 0; t < settings.dither[0]; t++)
                            {
                                maxrand += 1f / Math.Pow(results[starttile + t].Item1, pow);
                            }
                            var rand = rng.NextDouble() * maxrand;
                            for (var t = 0; t < settings.dither[0]; t++)
                            {
                                rand -= 1f / Math.Pow(results[starttile + t].Item1, pow);
                                if (rand <= 0)
                                {
                                    tile += t;
                                    break;
                                }
                            }
                        }
                        poster.PlaceImage(new Placement() { row = r, col = c, size = 1, img = tiles.tiles[results[tile].Item2] }, true);
                    }
                }
                var filename = $"{Path.GetFileNameWithoutExtension(settings.SourceImage)}_{settings.size}_{settings.colorspace}_{settings.weights[0]}{settings.weights[1]}{settings.weights[2]}_{pow}";
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Exporting {filename}");
                poster.Export(filename, settings.outputfolder);
            }
        }
    }
}
