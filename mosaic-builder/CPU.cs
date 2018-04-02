using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mosaic_builder
{
    class CPU
    {
        public static List<int> Run(int[] tiledata, int[] griddata, int pixelsPerTile, Settings settings)
        {
            var tilecount = tiledata.Count() / pixelsPerTile;
            var gridcount = griddata.Count() / pixelsPerTile;
            var scores = new int[tilecount * gridcount];

            var start = DateTime.Now;
            //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - CPU Started");
            var indexes = Enumerable.Range(0, tilecount * gridcount);
            Parallel.ForEach(indexes, index =>
            {
                int gridindex = index / tilecount;
                int tileindex = index % tilecount;
                if (gridindex < gridcount)
                {
                    int score = 0;
                    for (int tilepixel = 0; tilepixel < pixelsPerTile; tilepixel++)
                    {
                        score += pixel_diff(
                            tiledata[tileindex * pixelsPerTile + tilepixel],
                            griddata[gridindex * pixelsPerTile + tilepixel]
                        );
                    }
                    scores[index] = score;
                }
            });
            Timer.kernel += (DateTime.Now - start).TotalMilliseconds;
            //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Kernel Done, {timer / 1000}");

            return scores.ToList();
        }

        private static int pixel_diff(int a, int b)
        {
            return (
                    Math.Abs(((16711680 & a) - (16711680 & b)) >> 16)
                    + Math.Abs(((65280 & a) - (65280 & b)) >> 8)
                    + Math.Abs((255 & a) - (255 & b))
                    );
        }

    }
}
