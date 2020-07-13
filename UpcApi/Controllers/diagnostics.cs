using System;
using System.Web.Http;
using System.Web.Http.Cors;
using TCore.Logging;
using UpcShared;

namespace UpcApi.Controllers
{
    public class DiagnosticController : ApiController
    {
        [HttpGet]
        [Route("api/diagnostics/Heartbeat")]
        public IHttpActionResult Heartbeat()
        {
            USR_DiagnosticResult usrd = USR_DiagnosticResult.FromTCSR(USR.Success());

            usrd.TheValue = DiagnosticResult.ServiceRunning;

            return Ok(usrd);
        }
    }
}

