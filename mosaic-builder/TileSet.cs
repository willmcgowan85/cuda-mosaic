using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace mosaic_builder
{
    class TileSet
    {
        public List<Img> tiles;

        public TileSet(string folder)
        {
            var tiles = new ConcurrentBag<Img>();
            Parallel.ForEach(Directory.GetFiles(folder), new ParallelOptions() { MaxDegreeOfParallelism = 16 }, file =>
            {
                {
                    tiles.Add(new Img(file));
                }
            });
            this.tiles = tiles.ToList();
        }
    }
}
