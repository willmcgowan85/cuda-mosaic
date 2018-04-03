using ManagedCuda;
using ManagedCuda.BasicTypes;
using ManagedCuda.VectorTypes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace mosaic_builder
{
    class CUDA
    {
        public static List<int> Run(int[] tiledata, int[] griddata, int pixelsPerTile, Settings settings)
        {
            int[] scores = null;
            var folder = ConfigurationManager.AppSettings["CudaFolder"];

            try
            {
                CudaContext ctx = new CudaContext(settings.gpu);
                CUmodule cumodule = ctx.LoadModule(folder + ConfigurationManager.AppSettings["CudaFileName"]);
                var kernel = new CudaKernel(ConfigurationManager.AppSettings["CudaMethodName"], cumodule, ctx);

                var comparecount = (griddata.Count() / pixelsPerTile) * (tiledata.Count() / pixelsPerTile);
                var threads = kernel.MaxThreadsPerBlock;
                var blocks = comparecount / threads + 1;

                var maxgrids = ctx.GetDeviceInfo().MaxGridDim;
                var cores = (int)maxgrids.x;
                kernel.GridDimensions = new dim3((uint)(blocks > cores ? cores : blocks), (uint)(blocks > cores ? blocks / cores + 1 : 1), 1);
                kernel.BlockDimensions = threads;

                CudaDeviceVariable<int> grid_d = griddata;
                CudaDeviceVariable<int> tiles_d = tiledata;
                CudaDeviceVariable<int> scores_d = new CudaDeviceVariable<int>(comparecount);

                Timer.kernel += kernel.Run(
                    tiles_d.DevicePointer, 
                    grid_d.DevicePointer, 
                    tiledata.Count() / pixelsPerTile, 
                    griddata.Count() / pixelsPerTile, 
                    pixelsPerTile, 
                    scores_d.DevicePointer);

                scores = new int[comparecount];
                scores_d.CopyToHost(scores);

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