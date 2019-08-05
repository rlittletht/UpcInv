
using System.Net;
using System.Threading.Tasks;
using TCore.WebInterop;

namespace UpcApi.Proxy
{
    public class WebApi
    {
        private WebApiInterop m_apiInterop;

        public WebApi(WebApiInterop apiInterop)
        {
            m_apiInterop = apiInterop;
        }

        public async Task<USR_BookInfoList> GetBookScanInfosFromTitle(string Title)
        {
            string sQuery = $"api/book/GetBookScanInfosFromTitle?Title={Title ?? ""}";
            return await m_apiInterop.CallService<USR_BookInfoList>(sQuery, false);
        }

        void ThrowNYI()
        {
#if !DEBUG
            throw new Exception("NYI");
#endif
        }
    }
}