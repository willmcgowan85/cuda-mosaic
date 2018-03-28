using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG
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
