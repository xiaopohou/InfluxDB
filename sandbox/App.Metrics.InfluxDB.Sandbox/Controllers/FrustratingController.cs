using System;
using System.Threading.Tasks;
using App.Metrics.InfluxDB.Sandbox.JustForTesting;
using Microsoft.AspNetCore.Mvc;

namespace App.Metrics.InfluxDB.Sandbox.Controllers
{
    [Route("api/[controller]")]
    public class FrustratingController : Controller
    {
        private readonly RequestDurationForApdexTesting _durationForApdexTesting;

        private readonly IMetrics _metrics;

        public FrustratingController(IMetrics metrics, RequestDurationForApdexTesting durationForApdexTesting)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _durationForApdexTesting = durationForApdexTesting;
        }

        [HttpGet]
        public async Task<int> Get()
        {
            var duration = _durationForApdexTesting.NextFrustratingDuration;

            await Task.Delay(duration, HttpContext.RequestAborted);

            return duration;
        }
    }
}