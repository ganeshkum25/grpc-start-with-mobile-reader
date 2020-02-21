using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc_Api_Server.Protos;
using Grpc_Api_Server.Services;
using Microsoft.Extensions.Logging;

namespace Grpc_Api_Server.Service
{
    public class InternetUsageService : InternetDataUsageReaderService.InternetDataUsageReaderServiceBase
    {
        const double tolerance = 10e-6;

        private ILogger<InternetUsageService> _logger;

        public InternetUsageService(ILogger<InternetUsageService> logger)
        {
            _logger = logger;
        }

        private const double MaxDataUsage = 1024;
        public override Task<UsageLimitMessage> UpdateUsage(ReadingMessage request,
            ServerCallContext context)
        {
            try
            {
                var result = UsageLimitMessageThrowsException(request);
                return Task.FromResult(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        public override Task<UsageLimitPackage> UpdateUsages(ReadingPackage request,
            ServerCallContext context)
        {
            try
            {
                UsageLimitPackage result = new UsageLimitPackage();
                result.Note = "This is a common response and pick the response specific to customer.";

                foreach (var reading in request.Readings)
                {
                    var message = UsageLimitMessageThrowsException(reading);
                    result.Readings.Add(message);
                    //_logger.LogInformation($"\n\nreading for customer ID {reading.CustomerId} has received, \n " +
                    //                       $"with mobile number {reading.MobileNumber}\n" +
                    //                       $"has consumed {reading.DataUsage}\n\n");
                }

                return Task.FromResult(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        public override Task<UsageLimitPackage> SendDataUsage(ReadingPackage request,
            ServerCallContext context)
        {
            try
            {
                UsageLimitPackage result = new UsageLimitPackage();
                result.Note = "This is a common response and pick the response specific to customer.";

                foreach (var reading in request.Readings)
                {
                    var message = UsageLimitMessageThrowsException(reading);
                    result.Readings.Add(message);
                    //_logger.LogInformation($"\n\nreading for customer ID {reading.CustomerId} has received, \n " +
                    //                       $"with mobile number {reading.MobileNumber}\n" +
                    //                       $"has consumed {reading.DataUsage}\n\n");
                }

                return Task.FromResult(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        public override async Task<UsageLimitMessage> SendDataUsageOnStream(
            IAsyncStreamReader<ReadingMessage> requestStream,
            ServerCallContext context)
        {
            var task = Task.Run(
                async () =>
                {
                    await foreach (var reading in requestStream.ReadAllAsync())
                    {
                        _logger.LogInformation("Stream message received...");
                        _logger.LogInformation($"\n\nreading for customer ID {reading.CustomerId} has received, \n " +
                                               $"with mobile number {reading.MobileNumber}\n" +
                                               $"has consumed {reading.DataUsage}\n\n");
                    }
                });

            await task;
            return new UsageLimitMessage();
        }

        public override async Task DataUsageOnBiDirectionalStream(
            IAsyncStreamReader<ReadingMessage> requestStream,
            IServerStreamWriter<UsageLimitMessage> responseStream,
            ServerCallContext context)
        {
            //var tokenNumber = context.RequestHeaders.FirstOrDefault(k => k.Key == "Request-Token-number")?.Value;
            //_logger.LogInformation("Token Number is : " + tokenNumber);

            var task = Task.Run(
                async () =>
                {
                    await foreach (var reading in requestStream.ReadAllAsync())
                    {
                        try
                        {
                            _logger.LogInformation("Stream message received...");
                            _logger.LogInformation($"\n\nreading for customer ID {reading.CustomerId} has received, \n " +
                                                   $"with mobile number {reading.MobileNumber}\n" +
                                                   $"has consumed {reading.DataUsage}\n\n");


                            var usageLimitMessage = UsageLimitMessage(reading);

                            await responseStream.WriteAsync(usageLimitMessage);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                });

            await task;
        }

        private static UsageLimitMessage UsageLimitMessageThrowsException(ReadingMessage request)
        {
            var result = new UsageLimitMessage()
            {
                CustomerId = request.CustomerId,
                MobileNumber = request.MobileNumber
            };

            if (request.DataUsage < MaxDataUsage && request.ReadingTime.ToDateTime().ToUniversalTime() == DateTime.UtcNow)
            {
                result.ContinueUsage = ContinueService.Yes;
                result.RemainingData = 0;
            }
            else if (Math.Abs(request.DataUsage - MaxDataUsage) < tolerance && request.ReadingTime.ToDateTime().ToUniversalTime() == DateTime.UtcNow)
            {
                result.ContinueUsage = ContinueService.No;
                result.RemainingData = 0;
            }
            else
            {
                var additionalInfo = new Metadata()
                    {
                        new Metadata.Entry("Time",DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
                        new Metadata.Entry("exception_cause","Data usage exceeded."),
                    };

                throw new RpcException(new Status(StatusCode.Cancelled, "Data usage has reached at the peak, Can not continue the data usage."), additionalInfo);
            }

            return result;
        }

        private static UsageLimitMessage UsageLimitMessage(ReadingMessage request)
        {
            var result = new UsageLimitMessage()
            {
                CustomerId = request.CustomerId,
                MobileNumber = request.MobileNumber
            };

            if (request.DataUsage <= MaxDataUsage && request.ReadingTime.ToDateTime().ToUniversalTime() == DateTime.UtcNow)
            {
                result.ContinueUsage = ContinueService.Yes;
                result.RemainingData = 0;
            }
            else
            {
                result.ContinueUsage = ContinueService.No;
                result.RemainingData = 0;
            }

            return result;
        }
    }
}