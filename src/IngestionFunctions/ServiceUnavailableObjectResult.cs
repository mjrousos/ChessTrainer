using Microsoft.AspNetCore.Mvc;

namespace IngestionFunctions
{
    /// <summary>
    /// An IActionResult indicating a service is currently unavailable.
    /// </summary>
    public class ServiceUnavailableObjectResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceUnavailableObjectResult"/> class.
        /// </summary>
        /// <param name="value">Data giving more details on the reason the service is unavailable.</param>
        public ServiceUnavailableObjectResult(object value)
            : base(value) => StatusCode = 503;
    }
}
