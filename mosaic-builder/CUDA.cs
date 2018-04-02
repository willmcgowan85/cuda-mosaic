using ManagedCuda;
using ManagedCuda.BasicTypes;
using ManagedCuda.VectorTypes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace mosaic_builder
{
    class CUDA
    {
        public static List<int> Run(int[] tiledata, int[] griddata, int pixelsPerTile, Settings settings)
        {
            //var list = new List<int>();
            int[] scores = null;

            var folder = ConfigurationManager.AppSettings["CudaFolder"];

            try
            {
                CudaContext ctx = new CudaContext(settings.gpu);
                CUmodule cumodule = ctx.LoadModule(folder + ConfigurationManager.AppSettings["CudaFileName"]);

                var kernel = new CudaKernel(ConfigurationManager.AppSettings["CudaMethodName"], cumodule, ctx);

                var comparecount = (griddata.Count() / pixelsPerTile) * (tiledata.Count() / pixelsPerTile);
                //var threads = settings.threads > 0 && settings.threads < kernel.MaxThreadsPerBlock ? settings.threads : kernel.MaxThreadsPerBlock;
                var threads = kernel.MaxThreadsPerBlock;
                var blocks = comparecount / threads + 1;

                var maxgrids = ctx.GetDeviceInfo().MaxGridDim;
                var cores = (int)maxgrids.x;
                kernel.GridDimensions = new dim3((uint)(blocks > cores ? cores : blocks), (uint)(blocks > cores ? blocks / cores + 1 : 1), 1);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - dim = {threads}, {kernel.GridDimensions}");
                kernel.BlockDimensions = threads;

                CudaDeviceVariable<int> grid_d = griddata;
                CudaDeviceVariable<int> tiles_d = tiledata;
                CudaDeviceVariable<int> scores_d = new CudaDeviceVariable<int>(comparecount);

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Kernel Started");
                var resultk = kernel.Run(tiles_d.DevicePointer, grid_d.DevicePointer, tiledata.Count() / pixelsPerTile, griddata.Count() / pixelsPerTile, pixelsPerTile, scores_d.DevicePointer);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Kernel Done, {resultk / 1000}");

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Copying data to Host");
                scores = new int[comparecount];
                scores_d.CopyToHost(scores);

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Returning Results");
                var sb = new StringBuilder();
                //list.AddRange(scores);

                ctx.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
            }
            return scores.ToList();
        }
    }
}
