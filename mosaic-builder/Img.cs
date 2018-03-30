using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MosaicBuilder
{
    class Img
    {
        public List<Pixel> pixels = new List<Pixel>();
        public string file;

        public Img(string file) : this(new Bitmap(Image.FromFile(file)), file) { }

        public Img(Bitmap bitmap, string file = "")
        {
            this.file = file;
            foreach (var r in Enumerable.Range(0, bitmap.Height))
            {
                foreach (var c in Enumerable.Range(0, bitmap.Width))
                {
                    pixels.Add(new Pixel(bitmap.GetPixel(c, r)));
                }
            }
            bitmap.Dispose();
        }
    }
}
