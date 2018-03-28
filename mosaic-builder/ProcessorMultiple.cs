using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG
{
    class ProcessorMultiple : ProcessorBase
    {
        public override void CreateMosaic(Settings settings)
        {
            var tilewidth = 60;
            var tileheight = 44;
            //var dim = 64;
            settings.weights = GetWeights(settings.weightsStr);
            settings.dither = GetDither(settings.ditherStr);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Loading Tiles");
            var tiles = new TileSet(settings.FolderStr);
            var tile = tiles.tiles.First();

            //CreateMosaic(settings, dim, ColorSpace.XYZ, tiles, poster, tilewidth, tileheight);
            //var colors = new List<ColorSpace>()
            //{
            //    ColorSpace.CMY,
            //    ColorSpace.HSL,
            //    ColorSpace.HSV,
            //};
            var colors = new List<ColorSpace>()
            {
                ColorSpace.CMY,
                ColorSpace.HSL,
                ColorSpace.HSV,
                ColorSpace.LAB,
                //ColorSpace.LMS,
                ColorSpace.RGB,
                //ColorSpace.XYZ,
            };
            var sizes = new List<int>()
            {
                settings.size
            };
            foreach (ColorSpace cs in colors)
            {
                foreach (var size in sizes)
                {
                    CreateMosaic(settings, size, cs, tiles, tilewidth, tileheight);
                }
            }
        }

        private void CreateMosaic(Settings settings, int dim, ColorSpace cs, TileSet tiles, int tilewidth, int tileheight)
        {
            var pixelsPerTile = tilewidth * tileheight;
            settings.weights = GetWeights(settings.weightsStr);
            settings.dither = GetDither(settings.ditherStr);

            settings.colorspace = cs;

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Loading Tiles");
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
            List<Tuple<int, int>> results = null;
            results = CUDA.RunAdvanced(tiledata, griddata, pixelsPerTile, settings);
            poster.SetResults(results);
            //poster.ExportResults($"{Path.Combine(settings.outputfolder,Path.GetFileNameWithoutExtension(settings.SourceImage))}_{settings.size}_{settings.colorspace}_{settings.weights[0]}{settings.weights[1]}{settings.weights[2]}");

            for (var pow = settings.dither[1]; pow <= settings.dither[2]; pow += settings.dither[3])
            {
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Placing Tiles");
                var rng = new Random();
                for (var r = 0; r < poster.rows; r++)
                {
                    for (var c = 0; c < poster.cols; c++)
                    {
                        var tile = 0;
                        if (pow > 0)
                        {
                            var maxrand = 0.0d;
                            for (var t = 0; t < settings.dither[0]; t++)
                            {
                                maxrand += 1f / Math.Pow(poster.scoreslist[c][r][t].Item1, pow);
                            }
                            var rand = rng.NextDouble() * maxrand;
                            for (var t = 0; t < settings.dither[0]; t++)
                            {
                                rand -= 1f / Math.Pow(poster.scoreslist[c][r][t].Item1, pow);
                                if (rand <= 0)
                                {
                                    tile += t;
                                    break;
                                }
                            }
                        }
                        poster.PlaceImage(new Placement() { row = r, col = c, size = 1, img = tiles.tiles[poster.scoreslist[c][r][tile].Item2], score = poster.scoreslist[c][r][tile].Item1 }, true);
                    }
                }
                var filename = $"{Path.GetFileNameWithoutExtension(settings.SourceImage)}_{settings.size}_{settings.colorspace}_{settings.weights[0]}{settings.weights[1]}{settings.weights[2]}_{pow}";
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Exporting {filename}");
                poster.Export(filename, settings.outputfolder);
            }
        }
    }
}
