using Microsoft.Extensions.Hosting;
using ServiceWire.TcpIp;
using System.Threading;
using System.Threading.Tasks;
using XboxWheelWorker;
using XboxWheelTovJoyUI;
using Microsoft.Extensions.Logging;

namespace XboxWheelWorkerService
{
    public class Worker : BackgroundService, IWheelInputService
    {
        private readonly TcpHost TCPHost;
        private readonly ILogger<Worker> _logger;
        private WheelInputTask wheelInputTask;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            _logger.LogInformation("Initializing TCPHost and WheelInputTask.");

            try
            {
                TCPHost = new TcpHost(16581);
                wheelInputTask = new WheelInputTask();

                _logger.LogInformation("Successfully initialized TCPHost and WheelInputTask.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to initialize TCPHost and WheelInputTask.");
                throw; // rethrow the exception to stop the service from starting
            }
        }

        public async Task StartWheelInput()
        {
            await wheelInputTask.Start();
        }

        public void StopWheelInput()
        {
            wheelInputTask.Stop();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting worker.");

            TCPHost.AddService<IWheelInputService>(this);
            TCPHost.Open();

            // Start wheel input
            await StartWheelInput();

            _logger.LogInformation("Worker started.");

            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stopping worker.");

            TCPHost.Close();

            // Stop wheel input
            StopWheelInput();

            _logger.LogInformation("Worker stopped.");

            await Task.CompletedTask;
        }
    }
}
