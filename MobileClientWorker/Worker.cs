using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Grpc_Api_Server.Protos;
using Grpc_Api_Server.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ArgIterator = System.ArgIterator;

namespace MobileClientWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        private InternetDataUsageReaderService.InternetDataUsageReaderServiceClient _client = null;

        protected InternetDataUsageReaderService.InternetDataUsageReaderServiceClient Client
        {
            get
            {
                if (_client == null)
                {
                    var channel = GrpcChannel.ForAddress("http://localhost:55300");
                    _client = new InternetDataUsageReaderService.InternetDataUsageReaderServiceClient(channel);
                }

                return _client;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var usages = new ReadingPackage();
                for (int i = 0; i < 5; i++)
                {
                    var mobile1Usage = await MobileUsageRecorder.GetMessage();
                    usages.Readings.Add(mobile1Usage);
                }

                try
                {
                    var result = await Client.SendDataUsageAsync(usages);
                    foreach (var reading in result.Readings)
                    {
                        if (reading.ContinueUsage == ContinueService.Yes)
                        {
                            _logger.LogInformation("Continue services for Mobile Number " + reading.MobileNumber);
                        }
                        else
                        {
                            _logger.LogInformation("Stop services for Mobile Number " + reading.MobileNumber);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }


                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
