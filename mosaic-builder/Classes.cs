using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MTG
{
    class Classes
    {

        static string conn = "Server=(local);Database=MTG;Trusted_Connection=True;";
        static int m = 423668;
        static string folder = @"C:\mtg\";
        static string file = folder + m + ".jpg";
        static double xmin = 0.0738255033557047, ymin = 0.1105769230769231, xmax = 0.923489932885906, ymax = 0.5576923076923077;
        static string artfolder = @"C:\MTGArt\";


        static void ChopArt(string source, string dest)
        {
            Parallel.ForEach(Directory.GetFiles(artfolder + source), file =>
            {
                var image = System.Drawing.Image.FromFile(file);
                var rect1 = new Rectangle(0, 0, image.Width / 2, image.Height / 2);
                var rect2 = new Rectangle(image.Width / 2, 0, image.Width / 2, image.Height / 2);
                var rect3 = new Rectangle(0, image.Height / 2, image.Width / 2, image.Height / 2);
                var rect4 = new Rectangle(image.Width / 2, image.Height / 2, image.Width / 2, image.Height / 2);
                new Bitmap(image).Clone(rect1, image.PixelFormat).Save(artfolder + dest + Path.GetFileNameWithoutExtension(file) + "_a.jpg");
                new Bitmap(image).Clone(rect2, image.PixelFormat).Save(artfolder + dest + Path.GetFileNameWithoutExtension(file) + "_b.jpg");
                new Bitmap(image).Clone(rect3, image.PixelFormat).Save(artfolder + dest + Path.GetFileNameWithoutExtension(file) + "_c.jpg");
                new Bitmap(image).Clone(rect4, image.PixelFormat).Save(artfolder + dest + Path.GetFileNameWithoutExtension(file) + "_d.jpg");

            });
        }

        static void ResizeArt(string source, string dest)
        {
            Parallel.ForEach(Directory.GetFiles(artfolder + source), file => {
                var image = System.Drawing.Image.FromFile(file);
                var art = ResizeImage(image, 60, 44);
                art.Save(artfolder + dest + Path.GetFileName(file));
            });
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        static void ExportArt()
        {
            using (var cn = new SqlConnection(conn))
            {
                var cards = cn.GetAll<ImageDetails>();
                foreach (var card in cards)
                {
                    File.WriteAllBytes($"{folder}card_{card.multiverseId}.jpg", card.cardImage);
                    File.WriteAllBytes($"{folder}art_{card.multiverseId}.jpg", card.artwork);
                }
            }
        }

        static void CropArtwork()
        {
            var imagesToProcess = new List<ImageDetails>();
            var locker = new object();

            using (var cn = new SqlConnection(conn))
            {

                cn.Open();
                var reader = new SqlCommand("SELECT multiverseId, cardImage FROM Images", cn).ExecuteReader();
                while (reader.Read())
                {
                    imagesToProcess.Add(new ImageDetails() { multiverseId = (int)reader["multiverseId"], cardImage = (byte[])reader["cardImage"] });
                }
                cn.Close();
            }

            using (var cn = new SqlConnection(conn))
            {
                Parallel.ForEach(imagesToProcess, img =>
                {
                    var image = System.Drawing.Image.FromStream(new MemoryStream(img.cardImage));
                    var rect = new Rectangle((int)(image.Width * xmin), (int)(image.Height * ymin), (int)(image.Width * xmax - image.Width * xmin), (int)(image.Height * ymax - image.Height * ymin));
                    var art = new Bitmap(image).Clone(rect, image.PixelFormat);
                    using (var stream = new MemoryStream())
                    {
                        art.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                        img.artwork = stream.ToArray();
                    }
                    lock (locker)
                    {
                        cn.Update(img);
                        Console.WriteLine(img.multiverseId + " " + rect.ToString());
                    }
                });
            }
        }

        static void StoreArt()
        {
            using (var cn = new SqlConnection(conn))
            {
                cn.Update(new ImageDetails()
                {
                    multiverseId = 423668,
                    cardImage = File.ReadAllBytes(folder + m + ".jpg"),
                    artwork = File.ReadAllBytes(folder + m + ".art.jpg")
                });
            }
        }

        static void LoadImage()
        {
            var image = System.Drawing.Image.FromFile(file);
            var rect = new Rectangle((int)(image.Width * xmin), (int)(image.Height * ymin), (int)(image.Width * xmax - image.Width * xmin), (int)(image.Height * ymax - image.Height * ymin));
            var art = new Bitmap(image).Clone(rect, image.PixelFormat);
            art.Save(folder + m + ".art.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        static void SaveImage()
        {
            Directory.CreateDirectory(folder);
            using (var cn = new SqlConnection(conn))
            {
                var records = cn.GetAll<ImageDetails>();
                foreach (var record in records)
                {
                    File.WriteAllBytes(folder + m + ".jpg", record.cardImage);
                }
            }
        }

        private static void GetImage()
        {
            var source = $@"https://deckmaster.info/images/cards/AER/423668-hr.jpg";
            using (var cn = new SqlConnection(conn))
            {
                using (var wc = new WebClient())
                {
                    var image = new ImageDetails()
                    {
                        multiverseId = 423668,
                        cardImage = wc.DownloadData(source)
                    };
                    cn.Insert(image);
                }
            }
        }

        private static void GetImage(int i)
        {
            var source = $@"https://deckmaster.info/images/cards/AER/{i}-hr.jpg";
            using (var cn = new SqlConnection(conn))
            {
                using (var wc = new WebClient())
                {
                    var image = new ImageDetails()
                    {
                        multiverseId = i,
                        cardImage = wc.DownloadData(source)
                    };
                    cn.Insert(image);
                }
            }
        }
    }

    class ImageDetails
    {
        [ExplicitKey]
        public int multiverseId { get; set; }
        public byte[] cardImage { get; set; }
        public byte[] artwork { get; set; }

    }
}
