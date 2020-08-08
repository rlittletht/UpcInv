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

        public Client(string sAppTenant, string sAppID)
        {
            m_cred = new InteractiveBrowserCredential(sAppTenant, sAppID);
            m_client = new SecretClient(new Uri("https://upcinv.vault.azure.net/"), m_cred);
        }

        public async Task<string> GetSecret(string sSecretID)
        {
            Azure.Response<Azure.Security.KeyVault.Secrets.KeyVaultSecret> bundle = await m_client.GetSecretAsync(
                sSecretID);

            return bundle.Value.Value;
        }
    }
}