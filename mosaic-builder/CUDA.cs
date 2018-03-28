using ManagedCuda;
using ManagedCuda.BasicTypes;
using ManagedCuda.VectorTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG
{
    class CUDA
    {
        public static List<Tuple<int, int>> Run(int[] tiledata, int[] griddata, int pixelsPerTile, Settings settings)
        {
            var list = new List<Tuple<int, int>>();

            var folder = @"..\..\..\CUDA_9_0_176\x64\Debug\";
            //var folder = @".\CUDA\";

            try
            {
                CudaContext ctx = new CudaContext(settings.gpu);
                var info = ctx.GetDeviceInfo();
                Console.WriteLine($"File in Cuda folder: {Directory.GetFiles(folder).Count()}");
                CUmodule cumodule = ctx.LoadModule(folder + "kernel.ptx");


                var kernel = new CudaKernel("_Z6kernelPKiS0_iiPiS1_iiii", cumodule, ctx);

                var count = griddata.Length / pixelsPerTile;
                var maxblocks = ctx.GetDeviceInfo().MaxBlockDim;
                var maxgrids = ctx.GetDeviceInfo().MaxGridDim;
                var cores = (int)maxblocks.x;
                kernel.GridDimensions = new dim3((uint)(count > cores ? cores : count), (uint)(count > cores ? count / cores + 1 : 1), 1); // (int)Math.Ceiling((decimal)tiledata.Length / pixelsPerTile);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - kernel.GridDimensions = {kernel.GridDimensions}");
                kernel.BlockDimensions = kernel.MaxThreadsPerBlock;
                var checks = (int)Math.Ceiling((decimal)tiledata.Length / pixelsPerTile / kernel.MaxThreadsPerBlock);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - kernel checks per thread = {checks}");

                CudaDeviceVariable<int> grid_d = griddata;
                CudaDeviceVariable<int> tiles_d = tiledata;
                CudaDeviceVariable<int> scores_d = new CudaDeviceVariable<int>(count);
                CudaDeviceVariable<int> bests_d = new CudaDeviceVariable<int>(count);

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Kernel Started");
                kernel.Run(tiles_d.DevicePointer, grid_d.DevicePointer, checks, pixelsPerTile, scores_d.DevicePointer, bests_d.DevicePointer, tiledata.Length, griddata.Length, count, 1);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Kernel Done");

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Coping data to Host");
                var scores = new int[count];
                var bests = new int[count];
                scores_d.CopyToHost(scores);
                bests_d.CopyToHost(bests);

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Returning Results");
                for (var i = 0; i < scores.Length; i++)
                {
                    list.Add(new Tuple<int, int>(scores[i], bests[i]));
                }

                ctx.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine($"File in Cuda folder: ERROR");
            }
            return list;
        }

        public static List<Tuple<int, int>> RunAdvanced(int[] tiledata, int[] griddata, int pixelsPerTile, Settings settings)
        {
            var list = new List<Tuple<int, int>>();

            var folder = @"..\..\..\CUDA_9_0_176\x64\Debug\";
            //var folder = @".\CUDA\";

            try
            {
                CudaContext ctx = new CudaContext(settings.gpu);
                var info = ctx.GetDeviceInfo();
                //Console.WriteLine($"File in Cuda folder: {Directory.GetFiles(folder).Count()}");
                CUmodule cumodule = ctx.LoadModule(folder + "kernel.ptx");

                var kernel = new CudaKernel("_Z15kernel_advancedPKiS0_iiiPKfPiS3_", cumodule, ctx);

                var count = (griddata.Count() / pixelsPerTile) * (tiledata.Count() / pixelsPerTile);
                var threads = settings.threads > 0 && settings.threads < kernel.MaxThreadsPerBlock ? settings.threads : kernel.MaxThreadsPerBlock;
                //settings.threads = threads;
                var blocks = count / threads + 1;

                //var maxblocks = ctx.GetDeviceInfo().MaxBlockDim;
                var maxgrids = ctx.GetDeviceInfo().MaxGridDim;
                var cores = (int)maxgrids.x;
                kernel.GridDimensions = new dim3((uint)(blocks > cores ? cores : blocks), (uint)(blocks > cores ? blocks / cores + 1 : 1), 1);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - dim = {threads}, {kernel.GridDimensions}");
                kernel.BlockDimensions = threads;
                var checks = (int)Math.Ceiling((decimal)tiledata.Length / pixelsPerTile / threads);
                //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - kernel checks per thread = {checks}");

                CudaDeviceVariable<int> grid_d = griddata;
                CudaDeviceVariable<int> tiles_d = tiledata;
                CudaDeviceVariable<int> scores_d = new CudaDeviceVariable<int>(count);
                CudaDeviceVariable<int> bests_d = new CudaDeviceVariable<int>(count);
                CudaDeviceVariable<float> weights_d = settings.weights;

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Kernel Started");
                var resultk = kernel.Run(tiles_d.DevicePointer, grid_d.DevicePointer, tiledata.Count() / pixelsPerTile, griddata.Count() / pixelsPerTile, pixelsPerTile, weights_d.DevicePointer, scores_d.DevicePointer, bests_d.DevicePointer);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Kernel Done, {resultk / 1000}");

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Coping data to Host");
                var scores = new int[count];
                var bests = new int[count];
                scores_d.CopyToHost(scores);
                bests_d.CopyToHost(bests);

                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - Returning Results");
                var sb = new StringBuilder();
                for (var i = 0; i < scores.Length; i++)
                {
                    list.Add(new Tuple<int, int>(scores[i], bests[i]));
                    //sb.AppendLine($"{scores[i]},{bests[i]}");
                    //if(i < 20 || i % 1024 == 0 && i < 33000)
                    //{
                    //    Console.WriteLine($"{i,4} {scores[i],10} {bests[i],6}");
                    //}
                }
                //Directory.CreateDirectory(settings.outputfolder);
                //File.WriteAllText(Path.Combine(settings.outputfolder, $"{Path.GetFileNameWithoutExtension(settings.SourceImage)}_{settings.size}_{settings.colorspace}_{settings.weights[0]}{settings.weights[1]}{settings.weights[2]}_scores.csv"), sb.ToString());
                ctx.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
            }
            return list;
        }
    }
}
