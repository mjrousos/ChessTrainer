using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace IngestionFunctions
{
    public static class GameIngestion
    {
        [FunctionName("Function1")]
        public static void Run([ServiceBusTrigger("gamesToAnalyze", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
