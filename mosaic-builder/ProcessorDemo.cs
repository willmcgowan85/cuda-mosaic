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
            var tiledata = new int[tiles.tiles.Count * pixelsPerTile];

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Converting Tiles");
            Parallel.ForEach(Enumerable.Range(0, tiles.tiles.Count), t => {
                for (var p = 0; p < pixelsPerTile; p++)
                {
                    tiledata[t * pixelsPerTile + p] = tiles.tiles[t].pixels[p].AsInt(settings.colorspace);
                }
            });

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

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Done Converting");
            var results = CUDA.Run(tiledata, griddata, pixelsPerTile, settings);
            //var results = CPU.Run(tiledata, griddata, pixelsPerTile, settings);
            poster.SetResults(results, settings);

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
                        poster.PlaceImage(new Placement() {
                            row = r,
                            col = c,
                            size = 1,
                            img = tiles.tiles[poster.scoreslist[c][r][tile].Item2],
                            score = poster.scoreslist[c][r][tile].Item1
                        }, true);
                    }
                }
                var filename = $"{Path.GetFileNameWithoutExtension(settings.SourceImage)}_{settings.size}_{settings.colorspace}_{pow}";
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Exporting {filename}");
                poster.Export(filename, settings.outputfolder);
            }
        }
    }
}
