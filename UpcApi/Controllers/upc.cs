using System.Web.Http;
using TCore.Logging;

namespace UpcApi.Controllers
{
    public class UpcController : ApiController
    {
        [HttpGet]
        [Route("api/Upc/GetLastScanDate")]
        public IHttpActionResult GetLastScanDate(string ScanCode)
        {
            return Ok(UpcUpc.GetLastScanDate(ScanCode));
        }

        [HttpGet]
        [Route("api/Upc/UpdateUpcLastScanDate")]
        public IHttpActionResult UpdateUpcLastScanDate([FromUri] string ScanCode, [FromUri] string Title)
        {
            return Ok(UpcUpc.UpdateUpcLastScanDate(ScanCode, Title));
        }

        [HttpGet]
        [Route("api/Upc/FetchTitleFromGenericUPC")]
        public IHttpActionResult FetchTitleFromGenericUPC([FromUri] string Code)
        {
            return Ok(UpcUpc.FetchTitleFromGenericUPC(Code));
        }

        [HttpGet]
        [Route("api/Upc/FetchTitleFromISBN13")]
        public IHttpActionResult FetchTitleFromISBN13([FromUri] string Code)
        {
            return Ok(UpcUpc.FetchTitleFromISBN13(Code));
        }
    }
}