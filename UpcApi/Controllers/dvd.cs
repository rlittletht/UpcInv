using System.Web.Http;
using TCore.Logging;
using System;

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
        [Route("api/dvd/GetFullDvdScanInfo")]
        public IHttpActionResult GetFullDvdScanInfo(string ScanCode)
        {
            return Ok(UpcDvd.GetFullDvdScanInfo(ScanCode));
        }

        [HttpGet]
        [Route("api/dvd/GetDvdScanInfosFromTitle")]
        public IHttpActionResult GetDvdScanInfosFromTitle(string Title)
        {
            return Ok(UpcDvd.GetDvdScanInfosFromTitle(Title));
        }

        [HttpGet]
        [Route("api/dvd/QueryDvdScanInfos")]
        public IHttpActionResult GetDvdScanInfos(string Title, string Summary, string Since)
        {
            DateTime? dttm = string.IsNullOrEmpty(Since) ? (DateTime? ) null : DateTime.Parse(Since);

            return Ok(UpcDvd.QueryDvdScanInfos(Title, Summary, dttm));
        }

        [HttpGet]
        [Route("api/dvd/CreateDvd")]
        public IHttpActionResult CreateDvd([FromUri] string ScanCode, [FromUri] string Title)
        {
            return Ok(UpcDvd.CreateDvd(ScanCode, Title));
        }
    }
}