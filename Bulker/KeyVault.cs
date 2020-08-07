using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest.Azure;

namespace TCore.KeyVault
{
    class Client
    {
        private SecretClient m_client;
        private TokenCredential m_cred;

        public Client()
        {
            DefaultAzureCredentialOptions options = new DefaultAzureCredentialOptions();

            options.ExcludeEnvironmentCredential = true;
            options.ExcludeManagedIdentityCredential = true;
            options.ExcludeInteractiveBrowserCredential = false;
            options.ExcludeSharedTokenCacheCredential = false;

//            AzureServiceTokenProvider provider = new AzureServiceTokenProvider();
//            TokenCredentialOptions options2 = new TokenCredentialOptions();

//            options2.

            TokenCredentialOptions options2 = new TokenCredentialOptions();

            options2.AuthorityHost = new Uri("https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0");

            m_cred = new InteractiveBrowserCredential(
                "b90f9ef3-5e11-43e0-a75c-1f45e6b223fb" /*"9188040d-6c67-4c5b-b112-36a304b66dad"*/,
                "bfbaffd7-2217-4deb-a85a-4f697e6bdf94");// options2);
//                "589a6a0e-b169-479c-9c66-a2c2faff28bd", options2);
//            m_cred = new InteractiveBrowserCredential("9188040d-6c67-4c5b-b112-36a304b66dad", "3787c23b-aa7b-450a-83bc-f6b9f13631ff", options2);// null/*"9188040d-6c67-4c5b-b112-36a304b66dad"*/, "589a6a0e-b169-479c-9c66-a2c2faff28bd", options2);

            // m_cred = new DefaultAzureCredential(options);
            m_client = new SecretClient(new Uri("https://upcinv.vault.azure.net/"), m_cred);
        }

        public async Task<string> GetSecret(string sSecret)
        {
            //AccessToken token = await m_cred.GetTokenAsync(new TokenRequestContext(null), CancellationToken.None).ConfigureAwait(false);
            AccessToken token = await m_cred.GetTokenAsync(new TokenRequestContext(new string[] { "https://vault.azure.net/.default" }), CancellationToken.None).ConfigureAwait(false);



            Azure.Response<Azure.Security.KeyVault.Secrets.KeyVaultSecret> bundle = await m_client.GetSecretAsync(
                "Thetasoft-Azure-ConnectionString/324deaac388a480ab992ccef03072b61");

            return bundle.Value.Value;
        }
    }
}