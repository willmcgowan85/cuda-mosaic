using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mosaic_builder
{
    class CPU
    {
        public static List<int> Run(int[] tiledata, int[] griddata, int pixelsPerTile)
        {
            var tilecount = tiledata.Count() / pixelsPerTile;
            var gridcount = griddata.Count() / pixelsPerTile;
            var scores = new int[tilecount * gridcount];
            var start = DateTime.Now;
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
            return scores.ToList();
        }

        private static int pixel_diff(int a, int b)
        {
            return (((16711680 & a) - (16711680 & b)) >> 16) * (((16711680 & a) - (16711680 & b)) >> 16)
                    + (((65280 & a) - (65280 & b)) >> 8) * (((65280 & a) - (65280 & b)) >> 8)
                    + ((255 & a) - (255 & b)) * ((255 & a) - (255 & b));
        }
    }
}