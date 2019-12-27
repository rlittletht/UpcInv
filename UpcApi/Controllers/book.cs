using System;
using System.Web.Http;
using System.Web.Http.Cors;
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
        [Route("api/book/GetFullBookScanInfo")]
        public IHttpActionResult GetFullBookScanInfo(string ScanCode)
        {
            return Ok(UpcBook.GetFullBookScanInfo(ScanCode));
        }

        [HttpGet]
        [Route("api/book/GetBookScanInfosFromTitle")]
        public IHttpActionResult GetBookScanInfosFromTitle(string Title)
        {
            return Ok(UpcBook.GetBookScanInfosFromTitle(Title));
        }

        [HttpGet]
        [Route("api/book/QueryBookScanInfos")]
        public IHttpActionResult QueryBookScanInfos(string Title, string Author, string Series, string Summary)
        {
            return Ok(UpcBook.QueryBookScanInfos(Title, Author, Series, Summary, null));
        }

        [HttpGet]
        [Route("api/book/QueryBookScanInfosSince")]
        public IHttpActionResult QueryBookScanInfos(string Title, string Author, string Series, string Summary, string SinceDate)
        {
            DateTime dttm = DateTime.Parse(SinceDate);

            return Ok(UpcBook.QueryBookScanInfos(Title, Author, Series, Summary, dttm));
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
