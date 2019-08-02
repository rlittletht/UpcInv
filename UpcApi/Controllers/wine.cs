
using System.Web.Http;
using TCore.Logging;

namespace UpcApi.Controllers
{
    public class WineController: ApiController
    {
        [HttpGet]
        [Route("api/wine/GetWineScanInfo")]
        public IHttpActionResult GetWineScanInfo(string ScanCode)
        {
            return Ok(UpcWine.GetWineScanInfo(ScanCode));

        }
    }
}