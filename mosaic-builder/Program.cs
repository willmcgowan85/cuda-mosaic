using System;

namespace MosaicBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            Settings settings = null;
            try
            {
                settings = Args.Configuration.Configure<Settings>().CreateAndBind(args);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            switch (settings.ProcessorStr)
            {
                case ProcessType.Demo:
                    new ProcessorDemo().CreateMosaic(settings);
                    break;
            }
        }
    }
}
