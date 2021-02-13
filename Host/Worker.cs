using Contract;
using Host.AssemblyLoading;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Host
{
    public class Worker : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Starting plugin service");

            ExecutePlugin();
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Stopping plugin service");
            return base.StopAsync(cancellationToken);
        }
        public bool ExecutePlugin()
        {
            Console.WriteLine("Starting plugin");
            try
            {
                Module.Run<IModule>("Plugin", "Plugin",
                    (plugin) =>
                    {
                        plugin.Initialize();
                        plugin.Execute();
                        plugin.Close();
                    });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while executing plugin {ex.Message}");
            }
            return false;
        }
    }
}
