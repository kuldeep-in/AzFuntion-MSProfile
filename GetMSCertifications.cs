using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Identity.Client;
using Azure.Identity;
using Azure.Core;

namespace fn_msProfile
{
    public static class GetMSCertifications
    {
        [FunctionName("GetMSCertifications")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string clientId = Environment.GetEnvironmentVariable("ClientId");
            string tenantId = Environment.GetEnvironmentVariable("TenantId");
            string clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
            string authority = $"https://login.microsoftonline.com/{tenantId}";
            string authToken = string.Empty;

            string[] scopes = new string[] { $"https://graph.microsoft.com/.default" };
            //string[] scopes = new string[] { $"{clientId}/.default" };
            //string[] scopes = new string[] { $"{clientId}/.default" };
            //string[] scopes = { "user.read" };

            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(tenantId)
                //.WithAuthority(authority)
                .WithClientSecret(clientSecret)
                .Build();


            // Option 01 - using Authorize Code
            var authRequestUrl = confidentialClientApplication.GetAuthorizationRequestUrl(scopes);
            string authorizationCode = "authorizationCode";
            AuthenticationResult authResult = await confidentialClientApplication.AcquireTokenByAuthorizationCode(scopes, authorizationCode).ExecuteAsync();
            authToken = authResult.AccessToken;

            // Option 02 - Using TokenForClinet
            AuthenticationResult authResult1 = await confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync();
            authToken = authResult1.AccessToken;

            // Option 03 - Using ClientSecretCredential
            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var tokenRequestContext = new TokenRequestContext(scopes);
            authToken = clientSecretCredential.GetTokenAsync(tokenRequestContext).Result.Token;

            // Read token from any of the above 3 options
            Console.WriteLine("Access token: {0}", authToken);

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(authToken)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Token:{authToken}";

            return new OkObjectResult(responseMessage);
        }
    }
}
