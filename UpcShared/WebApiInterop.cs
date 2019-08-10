
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TCore.WebInterop
{
    public class WebApiInterop
    {
        public interface IAccessTokenProvider
        {
            string GetAccessToken();
            string GetAccessTokenForScope(string[] rgsScopes);
        }

        private HttpClient m_client;
        private string m_sClientAccessToken; // used to determine if we can reuse a client
        private string m_apiRoot;
        private IAccessTokenProvider m_accessTokenProvider;

        public WebApiInterop(string apiRoot, IAccessTokenProvider provider)
        {
            m_accessTokenProvider = provider;
            m_apiRoot = apiRoot;
        }

        /*----------------------------------------------------------------------------
            %%Function: ProcessResponse
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            General response code for all HttpResponses (for now, this is just
            looking for our special need-consent handshake)
        ----------------------------------------------------------------------------*/
        HttpResponseMessage ProcessResponse(HttpResponseMessage msg)
        {
            if (msg.StatusCode == HttpStatusCode.Unauthorized)
            {
                // check to see if we just need to get consent
                foreach (AuthenticationHeaderValue val in msg.Headers.WwwAuthenticate)
                {
                    if (val.Scheme == "need-consent")
                    {
                        // the parameter is the URL the user needs to visit in order to grant consent. Construct
                        // a link to report to the user (here we can inject HTML into our DIV to make
                        // this easier).

                        // This is not the best user experience (they end up in a new tab to grant consent, 
                        // and that tab is orphaned... but for now it makes it clear how a flow *could*
                        // work)
                        throw new Exception(
                            $"The current user has not given the WebApi consent to access the Microsoft Graph on their behalf. <a href='{val.Parameter}' target='_blank'>Click Here</a> to grant consent.");
                    }
                }
            }

            if (msg.StatusCode == HttpStatusCode.InternalServerError || msg.StatusCode == HttpStatusCode.NotFound)
                throw new Exception(msg.ReasonPhrase);

            return msg;
        }

        /*----------------------------------------------------------------------------
            %%Function: GetServiceResponse
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse
           
            call the webapi and get the response. broke this out because its nice
            to be able to collect all the async calls in the same place.

            would be really nice to use await in here, but every time i try it, i get
            threading issues, so old school task it is. 
        ----------------------------------------------------------------------------*/
        async Task<HttpResponseMessage> GetServiceResponse(HttpClient client, string sTarget)
        {
            return await client.GetAsync(sTarget);
        }

        /*----------------------------------------------------------------------------
            %%Function: GetServicePostResponse
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Do an http post and get the response
        ----------------------------------------------------------------------------*/
        async Task<HttpResponseMessage> GetServicePostResponse(HttpClient client, string sTarget, HttpContent content)
        {
            return await client.PostAsync(sTarget, content);
        }

        /*----------------------------------------------------------------------------
            %%Function: GetServicePutResponse
         	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Do an http put and get the response
        ----------------------------------------------------------------------------*/
        async Task<HttpResponseMessage> GetServicePutResponse(HttpClient client, string sTarget, HttpContent content)
        {
            return await client.PutAsync(sTarget, content);
        }

        bool FNeAccessToken(string s1, string s2)
        {
            if (s1 == null && s2 == null)
                return false;

            if (s1 == null || s2 == null)
                return true;

            return String.Compare(s1, s2, StringComparison.Ordinal) != 0;
        }

        /*----------------------------------------------------------------------------
            %%Function: HttpClientCreate
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            setup the http client for the webapi calls we're going to make
        ----------------------------------------------------------------------------*/
        HttpClient HttpClientCreate(string sAccessToken)
        {
            if (m_client == null || FNeAccessToken(sAccessToken, m_sClientAccessToken))
            {
                HttpClient client = new HttpClient();

                // we have setup our webapi to take Bearer authentication, so add our access token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sAccessToken);

                if (m_client != null)
                    m_client.Dispose();

                m_client = client;
                m_sClientAccessToken = sAccessToken;
            }

            return m_client;
        }

        #region Helper Service Calls

        /*----------------------------------------------------------------------------
            %%Function: CallService
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Core call service, returning an httpresponse
        ----------------------------------------------------------------------------*/
        public async Task<HttpResponseMessage> CallService(string sTarget, bool fRequireAuth)
        {
            string sAccessToken = fRequireAuth ? m_accessTokenProvider.GetAccessToken() : null;
            if (sAccessToken == null && fRequireAuth == true)
                throw new Exception("Authentication failed, no access token");

            HttpClient client = HttpClientCreate(sAccessToken);

            return await GetServiceResponse(client, $"{m_apiRoot}/{sTarget}");
        }

        public async Task<HttpResponseMessage> CallServiceEx(string sTarget, bool fRequireAuth, string[] rgsScopes)
        {
            string sAccessToken = fRequireAuth ? m_accessTokenProvider.GetAccessTokenForScope(rgsScopes) : null;
            if (sAccessToken == null && fRequireAuth == true)
                throw new Exception("Authentication failed, no access token");

            HttpClient client = HttpClientCreate(sAccessToken);

            return await GetServiceResponse(client, $"{sTarget}");
        }

        /*----------------------------------------------------------------------------
            %%Function: CallService
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Call the service and parse the return value into the given type T        	
        ----------------------------------------------------------------------------*/
        public async Task<T> CallService<T>(string sTarget, bool fRequireAuth)
        {
            HttpResponseMessage resp = await CallService(sTarget, fRequireAuth);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Service returned 'user is unauthorized'");
            }

            string sJson = GetContentAsString(resp);

            return JsonConvert.DeserializeObject<T>(sJson);
        }

        /*----------------------------------------------------------------------------
            %%Function: CallService
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Call the service and parse the return value into the given type T        	
        ----------------------------------------------------------------------------*/
        public async Task<T> CallServiceEx<T>(string sTarget, bool fRequireAuth, string[] rgsScopes)
        {
            HttpResponseMessage resp = await CallServiceEx(sTarget, fRequireAuth, rgsScopes);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Service returned 'user is unauthorized'");
            }

            string sJson = GetContentAsString(resp);

            return JsonConvert.DeserializeObject<T>(sJson);
        }

        /*----------------------------------------------------------------------------
            %%Function: CallServicePut
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Call the service with a put, with the given HttpContent        	
        ----------------------------------------------------------------------------*/
        public async Task<HttpResponseMessage> CallServicePut(string sTarget, HttpContent content, bool fRequireAuth)
        {
            string sAccessToken = m_accessTokenProvider.GetAccessToken();
            if (sAccessToken == null && fRequireAuth == true)
                throw new Exception("Authentication failed, no access token");

            HttpClient client = HttpClientCreate(sAccessToken);

            return await GetServicePutResponse(client, $"{m_apiRoot}/{sTarget}", content);
        }

        /*----------------------------------------------------------------------------
            %%Function: CallServicePut
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Call the service put, and parse the result into the given type T
        ----------------------------------------------------------------------------*/
        public async Task<T> CallServicePut<T>(string sTarget, HttpContent content, bool fRequireAuth)
        {
            HttpResponseMessage resp = await CallServicePut(sTarget, content, fRequireAuth);

            string sJson = GetContentAsString(resp);

            return JsonConvert.DeserializeObject<T>(sJson);
        }

        /*----------------------------------------------------------------------------
            %%Function: CallServicePost
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Core call put service, returning an httpresponse
        ----------------------------------------------------------------------------*/
        public async Task<HttpResponseMessage> CallServicePost(string sTarget, HttpContent content, bool fRequireAuth)
        {
            string sAccessToken = m_accessTokenProvider.GetAccessToken();
            if (sAccessToken == null && fRequireAuth == true)
                throw new Exception("Authentication failed, no access token");

            HttpClient client = HttpClientCreate(sAccessToken);

            return await GetServicePostResponse(client, $"{m_apiRoot}/{sTarget}", content);
        }

        /*----------------------------------------------------------------------------
            %%Function: CallServicePost
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            Call the service put, and parse the result into the given type T
        ----------------------------------------------------------------------------*/
        public async Task<T1> CallServicePost<T1, T2>(string sTarget, T2 t2, bool fRequireAuth)
        {
            string s = JsonConvert.SerializeObject(t2);

            HttpContent content = new StringContent(s, Encoding.UTF8, "application/json");
            HttpResponseMessage resp = await CallServicePost(sTarget, content, fRequireAuth);

            string sJson = GetContentAsString(resp);

            return JsonConvert.DeserializeObject<T1>(sJson);
        }

        #endregion

        /*----------------------------------------------------------------------------
            %%Function: GetContentAsString
        	%%Qualified: TCore.MsalWeb.WebApiInterop.ProcessResponse

            convert the HttpResponseMessage into a string
        ----------------------------------------------------------------------------*/
        public string GetContentAsString(HttpResponseMessage resp)
        {
            Task<string> tskString = resp.Content.ReadAsStringAsync();

            tskString.Wait();
            return tskString.Result;
        }

        public string GetAccessTokenForScope(string[] rgs)
        {
            return m_accessTokenProvider.GetAccessTokenForScope(rgs);
        }
    }
}
