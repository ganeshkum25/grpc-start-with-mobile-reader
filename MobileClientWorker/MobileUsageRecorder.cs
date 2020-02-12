﻿using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc_Api_Server.Services;

namespace MobileClientWorker
{
    public class MobileUsageRecorder
    {
        public static Task<ReadingMessage> GetMessage()
        {
            var reading =  new ReadingMessage()
            {
                CustomerId = 1,
                DataUsage = DateTime.Now.Ticks + new Random().Next(10),
                ReadingTime = Timestamp.FromDateTime(DateTime.UtcNow),
                MobileNumber = "9766676869"
            };

            return Task.FromResult(reading);
        }
    }
}