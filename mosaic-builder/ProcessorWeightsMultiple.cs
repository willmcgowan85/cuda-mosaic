using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG
{
    class ProcessorWeightsMultiple : ProcessorBase
    {
        public override void CreateMosaic(Settings settings)
        {
            var tilewidth = 60;
            var tileheight = 44;
            //var dim = 64;

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
                ColorSpace.LMS,
                ColorSpace.RGB,
                ColorSpace.XYZ,
            };
            var sizes = new List<int>()
            {
                settings.size
            };
            foreach (ColorSpace cs in colors)
            {
                foreach (var size in sizes)
                {
                    for (var a = 0; a <= 9; a++)
                    {
                        for (var b = 0; b <= 9; b++)
                        {
                            for (var c = 0; c <= 9; c++)
                            {
                                if ((a + b + c) == 9)
                                    CreateMosaic(settings, size, cs, tiles, tilewidth, tileheight, new float[] { a, b, c });
                            }
                        }
                    }
                }
            }
        }

        private void CreateMosaic(Settings settings, int dim, ColorSpace cs, TileSet tiles, int tilewidth, int tileheight, float[] weights)
        {
            var pixelsPerTile = tilewidth * tileheight;
            var locker = new object();

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Loading Poster");
            var poster = new Poster(settings.SourceImage, dim, dim, tilewidth, tileheight);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Converting Tiles");
            var tiledata = new int[tiles.tiles.Count * pixelsPerTile];
            //for (var t = 0; t < tiles.tiles.Count; t++)
            Parallel.ForEach(Enumerable.Range(0, tiles.tiles.Count), new ParallelOptions() { MaxDegreeOfParallelism = 8 }, t =>
                {
                    for (var p = 0; p < pixelsPerTile; p++)
                    {
                        var pixel = tiles.tiles[t].pixels[p].AsInt(cs);
                        lock (locker)
                        {
                            tiledata[t * pixelsPerTile + p] = pixel;
                        }
                    }
                }
            );

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Converting Poster");
            var griddata = new int[pixelsPerTile * poster.cols * poster.rows];
            //for (var r = 0; r < poster.rows; r++)
            Parallel.ForEach(Enumerable.Range(0, poster.rows), new ParallelOptions() { MaxDegreeOfParallelism = 8 }, r =>
            {
                for (var c = 0; c < poster.cols; c++)
                {
                    Bitmap image;
                    lock (locker)
                    {
                        image = poster.GetSubImage(c, r, 1);
                    }
                    var data = Helpers.GetArray(image, cs);
                    lock (locker)
                    {
                        for (var d = 0; d < data.Length; d++)
                        {
                            griddata[(r * poster.cols + c) * data.Length + d] = data[d];
                        }
                    }
                }
            }
            );
            
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Done Converting");
            settings.weights = weights;
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
                var filename = $"{Path.GetFileNameWithoutExtension(settings.SourceImage)}_{dim}_{cs}_{weights[0]}{weights[1]}{weights[2]}_{pow}";
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Exporting {filename}");
                poster.Export(filename, settings.outputfolder);
            }
        }
    }
}
