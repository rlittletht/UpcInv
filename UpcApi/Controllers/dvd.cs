using System.Web.Http;
using TCore.Logging;

namespace UpcApi.Controllers
{
    public class DvdController : ApiController
    {
        [HttpGet]
        [Route("api/dvd/GetDvdScanInfo")]
        public IHttpActionResult GetDvdScanInfo(string ScanCode)
        {
            return Ok(UpcDvd.GetDvdScanInfo(ScanCode));
        }

        [HttpGet]
        [Route("api/dvd/GetDvdScanInfosFromTitle")]
        public IHttpActionResult GetDvdScanInfosFromTitle(string Title)
        {
            return Ok(UpcDvd.GetDvdScanInfosFromTitle(Title));
        }

        [HttpGet]
        [Route("api/dvd/CreateDvd")]
        public IHttpActionResult CreateDvd([FromUri] string ScanCode, [FromUri] string Title)
        {
            return Ok(UpcDvd.CreateDvd(ScanCode, Title));
        }
    }
}