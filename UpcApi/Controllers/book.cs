using System.Web.Http;
using TCore.Logging;

namespace UpcApi.Controllers
{
    public class BookController : ApiController
    {
        [HttpGet]
        [Route("api/book/GetBookScanInfo")]
        public IHttpActionResult GetBookScanInfo(string ScanCode)
        {
            return Ok(UpcBook.GetBookScanInfo(ScanCode));
        }

        [HttpGet]
        [Route("api/book/GetBookScanInfosFromTitle")]
        public IHttpActionResult GetBookScanInfosFromTitle(string Title)
        {
            return Ok(UpcBook.GetBookScanInfosFromTitle(Title));
        }

        [HttpGet]
        [Route("api/book/CreateBook")]
        public IHttpActionResult CreateBook([FromUri] string ScanCode, [FromUri] string Title, [FromUri] string Location)
        {
            return Ok(UpcBook.CreateBook(ScanCode, Title, Location));
        }

        [HttpGet]
        [Route("api/book/UpdateBookScan")]
        public IHttpActionResult UpdateBookScan([FromUri] string ScanCode, [FromUri] string Title, [FromUri] string Location)
        {
            return Ok(UpcBook.UpdateBookScan(ScanCode, Title, Location));
        }

    }
}
