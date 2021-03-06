using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc_Api_Server.Protos;
using Grpc_Api_Server.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MobileClientWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        private InternetDataUsageReaderService.InternetDataUsageReaderServiceClient _client = null;
        private ILoggerFactory _loggerFactory;

        protected InternetDataUsageReaderService.InternetDataUsageReaderServiceClient Client
        {
            get
            {
                if (_client == null)
                {
                    try
                    {
                        var opts = new GrpcChannelOptions() { LoggerFactory = _loggerFactory };
                        var channel = GrpcChannel.ForAddress("http://localhost:55300", opts);
                        _client = new InternetDataUsageReaderService.InternetDataUsageReaderServiceClient(channel);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }

                return _client;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var counter = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker {counter} running at: {time}", counter, DateTimeOffset.Now);
                counter++;

                #region packet of messages

                //var usages = new ReadingPackage();
                //for (int i = 0; i < 5; i++)
                //{
                //    var mobile1Usage = await MobileUsageRecorder.GetMessage();
                //    usages.Readings.Add(mobile1Usage);
                //}

                //try
                //{
                //    var result = await Client.SendDataUsageAsync(usages, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(5)));
                //    foreach (var reading in result.Readings)
                //    {
                //        if (reading.ContinueUsage == ContinueService.Yes)
                //        {
                //            _logger.LogInformation("Continue services for Mobile Number " + reading.MobileNumber);
                //        }
                //        else
                //        {
                //            _logger.LogInformation("Stop services for Mobile Number " + reading.MobileNumber);
                //        }
                //    }
                //}
                //catch (RpcException rpcException)
                //{
                //    _logger.LogError(rpcException.Status.Detail);
                //    foreach (var additionalInfo in rpcException.Trailers)
                //    {
                //        _logger.LogError($"Extra information:\n Key = {additionalInfo.Key}\n value = {additionalInfo.Value}\n\n");
                //    }
                //}
                //catch (Exception e)
                //{
                //    _logger.LogError(e.ToString());
                //    throw;
                //}

                #endregion

                #region stream

                //_logger.LogInformation("Stream started here...");

                //var stream = Client.SendDataUsageOnStream();
                //for (int i = 0; i < 5; i++)
                //{
                //    _logger.LogInformation("Stream message sent...");

                //    var readingMessage = await MobileUsageRecorder.GetMessage();
                //    await stream.RequestStream.WriteAsync(readingMessage);
                //}

                //await stream.RequestStream.CompleteAsync();

                //_logger.LogInformation("Stream ends here...");

                #endregion

                #region Bi-directional streaming

                _logger.LogInformation("Stream started here...");

                //var metadata = new Metadata
                //{
                //    { "Request-Token-number", new Random().Next(1000).ToString()}
                //};


                var biDirectionalStream = Client.DataUsageOnBiDirectionalStream(/*metadata*/);


                var responseReaderTask = Task.Run(async () =>
                {
                    while (await biDirectionalStream.ResponseStream.MoveNext())
                    {
                        var usageLimitMessage = biDirectionalStream.ResponseStream.Current;

                        if (usageLimitMessage.ContinueUsage == ContinueService.Yes)
                        {
                            _logger.LogInformation("Continue services for Mobile Number " + usageLimitMessage.MobileNumber);
                        }
                        else
                        {
                            _logger.LogInformation("Stop services for Mobile Number " + usageLimitMessage.MobileNumber);
                        }

                    }
                });

                for (int i = 0; i < 5; i++)
                {
                    _logger.LogInformation("Stream message sent...");

                    var readingMessage = await MobileUsageRecorder.GetMessage();
                    await biDirectionalStream.RequestStream.WriteAsync(readingMessage);
                }

                await biDirectionalStream.RequestStream.CompleteAsync();
                await responseReaderTask;

                _logger.LogInformation("Stream ends here...");


                #endregion


                // How to send metadata (For authentication/authorization)
                // How to terminate Rpc

                Console.WriteLine("\n\n\nService execution stops\n\n\n");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
