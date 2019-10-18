using log4net;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AzSignalR.Monitor
{
    public class AzureHelper
    {
        private string TenantId { get; }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AzureHelper));

        public AzureHelper(string tenantId)
        {
            TenantId = tenantId;
        }

        public async Task<string> GetSecretValue(string keyVaultAddress, string secretName)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            using (var keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)))
            {
                Microsoft.Azure.KeyVault.Models.SecretBundle secret = null;
                try
                {
                    secret = await keyVaultClient.GetSecretAsync(keyVaultAddress, secretName)
                        .ConfigureAwait(false);
                    return secret.Value;
                }
                catch (Exception e)
                {
                    Logger.Error($"Error: fail to get secret '{secretName}' for {e}");
                }
                return null;
            }
        }

        public async Task<X509Certificate2> GetCertificateAsync(string keyVaultAddress, string certificateName)
        {
            var tokenProvider = new AzureServiceTokenProvider();
            using (var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback)))
            {
                var secretBundle = await client.GetSecretAsync(keyVaultAddress, certificateName);
                var privateKeyBytes = Convert.FromBase64String(secretBundle.Value);
                var cert = new X509Certificate2(privateKeyBytes, (string)null, X509KeyStorageFlags.MachineKeySet);
                return cert;
            }
        }

        public CloudTable GetTable(string tableName, CloudStorageAccount storageAccount)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            return table;
        }

        public async Task<IAzure> GetAzure(string subscriptionId)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/")
                .ConfigureAwait(false);
            var serviceCreds = new TokenCredentials(accessToken);
            var credentials = new AzureCredentials(serviceCreds, serviceCreds,
                TenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Configure().Authenticate(credentials).WithSubscription(subscriptionId);
            return azure;
        }
    }
}
