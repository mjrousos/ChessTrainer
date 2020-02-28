using System;
using System.Net;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;

namespace IngestionFunctions
{
    /// <summary>
    /// Polly HTTP policies for use with the ChessTrainer ingestion functions.
    /// </summary>
    public static class HttpPolicies
    {
        /// <summary>
        /// Gets a Polly HTTP policy that retries failed requests (including 429 responses) up to three times (3, 6, and 9 seconds pauses).
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> RetryPolicy
        {
            get => HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                    .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i * 3));
        }

        /// <summary>
        /// Gets a Polly HTTP policy that breaks outgoing HTTP connects for 30 seconds after 5 consecutive failures.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy
        {
            get => HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }
    }
}
