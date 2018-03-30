using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MosaicBuilder
{

    class Poster
    {
        public int tilewidth, tileheight, cols, rows, ver = 0;
        public Bitmap bitmap;
        public int?[][] scores;
        public Img[][] images;
        public List<Tuple<int, int>>[][] scoreslist;

        public Poster(string filename, int cols, int rows, int tilewidth, int tileheight)
        {
            this.tilewidth = tilewidth;
            this.tileheight = tileheight;

            var bitmap = new Bitmap(Image.FromFile(filename));
            var w = bitmap.Width;
            var h = bitmap.Height;
            var w_ratio = (float)w / cols / tilewidth;
            var h_ratio = (float)h / rows / tileheight;

            var max_ratio = w_ratio > h_ratio ? w_ratio : h_ratio;
            var w_max = max_ratio * cols * tilewidth;
            var h_max = max_ratio * rows * tileheight;

            var rows_new = (int)(rows * h / h_max);
            var cols_new = (int)(cols * w / w_max);

            var w_new = (int)(max_ratio * cols_new * tilewidth);
            var h_new = (int)(max_ratio * rows_new * tileheight);

            var left = (int)((w - w_new) / 2.0);
            var upper = (int)((h - h_new) / 2.0);
            var right = (int)(w - (w - w_new) / 2.0);
            var lower = (int)(h - (h - h_new) / 2.0);
            bitmap = bitmap.Clone(new Rectangle(left, upper, right - left, lower - upper), bitmap.PixelFormat);

            this.rows = rows_new;
            this.cols = cols_new;
            this.bitmap = Helpers.ResizeImage(bitmap, cols_new * tilewidth, rows_new * tileheight);

            scores = new int?[cols][];
            scoreslist = new List<Tuple<int, int>>[cols][];
            for (var i = 0; i < scores.Length; i++)
            {
                scores[i] = new int?[rows];
                scoreslist[i] = new List<Tuple<int, int>>[rows];
            }
            images = new Img[cols_new][];
            for(var c = 0; c < cols_new; c++)
            {
                images[c] = new Img[rows_new];
            }
        }

        public Bitmap GetSubImage(int col, int row, int size)
        {
            var left = col * tilewidth;
            var top = row * tileheight;
            var right = (col + size) * tilewidth;
            var bottom = (row + size) * tileheight;
            return bitmap.Clone(new Rectangle(left, top, right - left, bottom - top), bitmap.PixelFormat);
        }

        internal void SetResults(List<int> results, Settings settings)
        {
            int tilewidth = results.Count / rows / cols;
            foreach(var c in Enumerable.Range(0, cols))
            {
                foreach(var r in Enumerable.Range(0, rows))
                {
                    var imageindex = 0;
                    scoreslist[c][r] = new List<Tuple<int, int>>();
                    scoreslist[c][r].AddRange(results.GetRange((r * cols + c) * tilewidth, tilewidth).Select(e => new Tuple<int, int>(e, imageindex++)).OrderBy(e => e.Item1).Take(settings.dither[0]));
                }
            }
        }

        public bool CanPlace(int col, int row, int size)
        {
            for (var c = col; c < col + size; c++)
            {
                for (var r = row; r < row + size; r++)
                {
                    if (scores[c][r] != null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool CanPlace(Placement p)
        {
            return CanPlace(p.col, p.row, p.size);
        }

        public void PlaceImage(Placement p, bool forcePlacement = false)
        {
            if (CanPlace(p) || forcePlacement)
            {
                for (var c = p.col; c < p.col + p.size; c++)
                {
                    for (var r = p.row; r < p.row + p.size; r++)
                    {
                        scores[c][r] = p.score;
                    }
                }
                Graphics g = Graphics.FromImage(bitmap);
                g.DrawImageUnscaled(new Bitmap(Image.FromFile(p.img.file)), p.col * tilewidth, p.row * tileheight);
                g.Dispose();
                images[p.col][p.row] = p.img;
                //Export();
            }
        }

        public void Export(string output, string folder)
        {
            Directory.CreateDirectory(folder);
            var f = Path.Combine(folder, $"{output}__{DateTime.Now.ToString("yyMMdd_HHmmss")}");
            bitmap.Save(f + ".png", ImageFormat.Png);
            var sb = new StringBuilder();
            sb.AppendLine($"col,row,file,score");
            for (var r = 0; r < rows; r++)
            {
                for(var c = 0; c < cols; c++)
                {
                    if(images[c][r] != null)
                    {
                        sb.AppendLine($"{c},{r},{images[c][r].file},{scores[c][r]}");
                    }
                }
            }
            File.WriteAllText(f + ".txt", sb.ToString());
        }

        public bool HasAvailableCell()
        {
            return false;
        }

        public void ExportResults(string filename)
        {
            var sb = new StringBuilder();
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    foreach(var t in scoreslist[c][r])
                    {
                        sb.AppendLine($"{t.Item1},{t.Item2}");
                    }
                }
            }
            File.WriteAllText(filename + ".csv", sb.ToString());
        }
    }
}
