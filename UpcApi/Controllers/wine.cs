
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

        [HttpGet]
        [Route("api/wine/DrinkWine")]
        public IHttpActionResult DrinkWine(
            [FromUri] string ScanCode,
            [FromUri] string Wine,
            [FromUri] string Vintage,
            [FromUri] string Notes)
        {
            return Ok(UpcWine.DrinkWine(ScanCode, Wine, Vintage, Notes));
        }
    }
}