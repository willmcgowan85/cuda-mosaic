using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG
{
    abstract class ProcessorBase
    {
        public abstract void CreateMosaic(Settings settings);

        private int? GetScore(Img img1, Img img2, int break_out)
        {
            var score = 0;
            for (var i = 0; i < img1.pixels.Count; i++)
            {
                score += (img1.pixels[i].a - img2.pixels[i].a) * (img1.pixels[i].a - img2.pixels[i].a)
                    + (img1.pixels[i].b - img2.pixels[i].b) * (img1.pixels[i].b - img2.pixels[i].b)
                    + (img1.pixels[i].c - img2.pixels[i].c) * (img1.pixels[i].c - img2.pixels[i].c);
                if (score >= break_out)
                {
                    return null;
                }
            }
            return score;
        }

        internal float[] GetWeights(string weightsStr)
        {
            try
            {
                var parts = weightsStr.Split(',');

                var a = float.Parse(parts[0]);
                var b = float.Parse(parts[1]);
                var c = float.Parse(parts[2]);

                var sum = a + b + c;
                return new float[] { a / sum, b / sum, c / sum, };

                var min = Math.Min(a, Math.Min(b, c));
                var max = Math.Max(a, Math.Max(b, c));

                var A = (float)Math.Round(a);
                var B = (float)Math.Round(b);
                var C = (float)Math.Round(c);

                return new float[] { A, B, C };
            }
            catch (Exception e)
            {
                return new float[] { 1, 1, 1 };
            }
        }

        internal int[] GetDither(string ditherStr)
        {
            try
            {
                var parts = ditherStr.Split(',');

                var a = int.Parse(parts[0]);
                var b = int.Parse(parts[1]);
                var c = int.Parse(parts[2]);
                var d = int.Parse(parts[3]);

                return new int[] { a, b, c, d };
            }
            catch (Exception e)
            {
                return new int[] { 1, 1, 1, 1 };
            }
        }
    }

    public enum ProcessType
    {
        Advanced,
        Simple,
        SteppedTile,
        OffsetTile,
        Multiple,
        //Dither,
        WeightsMultiple
    }
}
