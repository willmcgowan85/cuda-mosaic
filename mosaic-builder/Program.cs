using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace MTG
{
    class Program
    {
        static string folder = $@"C:\MTGArt\";

        static void Main(string[] args)
        {


            Settings settings = null;
            try
            {
                settings = Args.Configuration.Configure<Settings>().CreateAndBind(args);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            switch (settings.ProcessorStr)
            {
                case ProcessType.Simple:
                    new ProcessorSimple().CreateMosaic(settings);
                    break;
                case ProcessType.Advanced:
                    new ProcessorAdvanced().CreateMosaic(settings);
                    break;
                case ProcessType.Multiple:
                    new ProcessorMultiple().CreateMosaic(settings);
                    break;
                case ProcessType.WeightsMultiple:
                    new ProcessorWeightsMultiple().CreateMosaic(settings);
                    break;
            }

            return;

            ////CreateMosaicFull($@"C:\MTGArt\Input\img.jpg", 32, 32, 4, "whole");
            //var folders = new Dictionary<int, string>
            //{
            //    { 4, "whole" },
            //    { 2, "half" },
            //    { 1, "quarter" }
            //};
            ////CreateMosaic($@"C:\MTGArt\Input\img.jpg", $@"32_quarter", 32, 32, folders);
            ////CreateMosaic($@"C:\MTGArt\Input\img.jpg", $@"64_half", 64, 64, folders, 15, 11);
            ////CreateMosaic($@"C:\MTGArt\Input\img.jpg", $@"128_quarter", 128, 128, folders);
            ////Console.ReadLine();
            //CreateMosaicOverlap($@"C:\MTGArt\Input\img.jpg", $@"overlap_64", 64, 64, 4, folders[4], 15, 11);
        }

        //private static void CreateMosaicOverlap(string file, string output, int cols, int rows, int size, string subfolder, int tilewidth, int tileheight)
        //{
        //    var poster = new Poster(file, cols, rows, tilewidth, tileheight);
        //    poster.Export(output, settings.outputfolder);
        //    var tileset = new TileSet(Path.Combine(folder, subfolder));


        //    while (poster.HasAvailableCell())
        //    {

        //        // get list of all possible placements
        //        // get best placement
        //        // clear affected placements
        //    }

        //}

        //private static void CreateMosaicFull(string file, string output, int cols, int rows, int size, string subfolder,  int tilewidth, int tileheight)
        //{
        //    var poster = new Poster(file, cols, rows, tilewidth, tileheight);
        //    poster.Export(output, settings.outputfolder);
        //    var tileset = new TileSet(Path.Combine(folder, subfolder));
        //    ProcessFolder(poster, tileset, size, true);
        //}

        //private static void CreateMosaic(string file, string output, int cols, int rows, Dictionary<int, string> subfolders, int tilewidth, int tileheight)
        //{
        //    var poster = new Poster(file, cols, rows, tilewidth, tileheight);
        //    poster.Export(output, settings.outputfolder);
        //    foreach (var subfolder in subfolders.OrderByDescending(a => a.Key))
        //    {
        //        var tileset = new TileSet(Path.Combine(folder, subfolder.Value));
        //        ProcessFolder(poster, tileset, subfolder.Key);
        //    }
        //    poster.Export(output, settings.outputfolder);
        //}

        private static void ProcessFolder(Poster poster, TileSet tileset, int size, bool force = false)
        {
            var sectors = new List<Placement>();
            for (var r = 0; r < poster.rows - size + 1; r++)
            {
                for (var c = 0; c < poster.cols - size + 1; c++)
                {
                    if (poster.CanPlace(c, r, size))
                    {
                        sectors.Add(new Placement()
                        {
                            img = new Img(poster.GetSubImage(c, r, size)),
                            col = c,
                            row = r,
                            size = size
                        });
                    }
                }
            }
            var bests = new ConcurrentBag<Placement>();
            Parallel.ForEach(sectors, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, sector =>
            {
                Console.Write(sector.img.file);
                var best = GetBest(sector, tileset);
                bests.Add(best);
                Console.WriteLine($@"{best.size},{best.col,2},{best.row,2},{best.score,12},{best.img.file}");
            });
            if (force)
            {
                foreach (var p in bests.ToList().OrderByDescending(e => e.score))
                {
                    poster.PlaceImage(p, force);
                }
            }
            else
            {
                foreach (var p in bests.ToList().OrderBy(e => e.score))
                {
                    poster.PlaceImage(p);
                }
            }
        }

        private static Placement GetBest(Placement pc, TileSet tileset)
        {
            Placement best = new Placement() { score = int.MaxValue };
            foreach (var t in tileset.tiles)
            {
                var score = GetScore(pc.img, t, best.score);
                if (score != null && score < best.score)
                {
                    best.col = pc.col;
                    best.row = pc.row;
                    best.size = pc.size;
                    best.score = (int)score;
                    best.img = t;
                } 
            }
            return best;
        }

        private static int? GetScore(Img img1, Img img2, int best)
        {
            return 0;
        }

        
    }
    




    
}
