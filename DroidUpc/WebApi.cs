
using System.Net;

namespace UpcApi.Proxy
{
    public class WebApiInterop
    {

    }
    public class TcApiProxy
    {
//         private WebApiInterop m_apiInterop;

        public TcApiProxy(WebApiInterop apiInterop)
        {
            // m_apiInterop = apiInterop;
            HttpStatusCode c;


        }

        void ThrowNYI()
        {
#if !DEBUG
            throw new Exception("NYI");
#endif
        }
    }
}