using System;
using System.Web.Http;
using System.Web.Http.Cors;
using TCore.Logging;

namespace UpcApi.Controllers
{
    public class DiagnosticController : ApiController
    {
        enum DiagnosticResult
        {
            ServiceRunning
        }

        [HttpGet]
        [Route("api/diagnostics/Heartbeat")]
        public IHttpActionResult Heartbeat()
        {
            return Ok(DiagnosticResult.ServiceRunning);
        }
    }
}

