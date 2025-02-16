using Azure.Core;
using San.MDbLogging;

namespace San.MdbLogging.TestWorker
{
    public class Worker : BackgroundService, ILoggable
    {
        private readonly ISQLLogger<LogUpdatePrice, Worker> _logger;

        public Worker(ISQLLogger<LogUpdatePrice, Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Log(new LogUpdatePrice
                {
                    Message = "Start ExecuteAsync",
                    Level = "Information",
                    TimeStamp = DateTime.Now,
                    Exception = null,
                    RequestId = Guid.NewGuid(),
                    ActionName = null,
                    SourceName = null,
                    Metadata = null,
                    InsuranceType = null,
                    NationalCode = null,
                    Logger = null
                });
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
