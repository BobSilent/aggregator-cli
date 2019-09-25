using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace aggregator
{
    public class AzureFunctionOAuthHandler
    {
        private readonly ILogger _log;
        private readonly ExecutionContext _context;

        public AzureFunctionOAuthHandler(ILogger logger, ExecutionContext context)
        {
            _log = logger;
            _context = context;
        }

        public async Task<HttpResponseMessage> CallbackAsync(HttpRequestMessage req, CancellationToken cancellationToken)
        {
            var vssUrl = @"https://xxx.visualstudio.com/";
            var azureFunctionInstanceName = "yyy";
            var callbackUrl = $"https://{azureFunctionInstanceName}.azurewebsites.net/api/OAuth-Callback";
            var clientSecret = "";

            _log.LogDebug($"Context: {_context.InvocationId} {_context.FunctionName} {_context.FunctionDirectory} {_context.FunctionAppDirectory}");

            var parameters = req.RequestUri.ParseQueryString();
            var code = parameters["code"];
            var state = parameters["state"];
            _log.LogInformation($"Callback Code {code} State {state}");

            var accessRequestBody = GenerateRequestPostData(clientSecret, code, callbackUrl);
            var tokenDetails = await GetAccessToken(accessRequestBody);

            _log.LogInformation($"Connecting to Azure DevOps using {tokenDetails.TokenType} (Expires in {TimeSpan.FromSeconds(tokenDetails.ExpiresInSec)}) for scopes={string.Join(";", tokenDetails.Scopes)}...");
            var clientCredentials = new VssOAuthAccessTokenCredential(tokenDetails.AccessToken);

            using (var devops = new VssConnection(new Uri(vssUrl), clientCredentials))
            {
                await devops.ConnectAsync(cancellationToken);
                _log.LogInformation($"Connected to Azure DevOps");
                using (var clientsContext = new AzureDevOpsClientsContext(devops))
                {
                    var content = await clientsContext.WitClient.GetWorkItemAsync(2176, cancellationToken: cancellationToken);
                    _log.LogInformation(JsonConvert.SerializeObject(content));

                    return req.CreateResponse(HttpStatusCode.OK);
                }
            }
        }


        private string GenerateRequestPostData(string appSecret, string authCode, string callbackUrl)
        {
            return $"client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer" +
                   $"&client_assertion={HttpUtility.UrlEncode(appSecret)}" +
                   $"&grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer&assertion={HttpUtility.UrlEncode(authCode)}" +
                   $"&redirect_uri={callbackUrl}";
        }


        private async Task<OAuthTokenDetails> GetAccessToken(string body)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"https://app.vssps.visualstudio.com");

                _log.LogInformation("HttpRequestMessage");

                var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                _log.LogInformation("PostAsync");
                using (var response = await client.PostAsync(@"/oauth2/token", content))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        _log.LogInformation($"SendAsync Result {result}");

                        OAuthTokenDetails details = JsonConvert.DeserializeObject<OAuthTokenDetails>(result);
                        return details;
                    }

                    _log.LogInformation($"SendAsync StatusCode {response.StatusCode}");
                }
            }

            return new OAuthTokenDetails();
        }
    }


    public class OAuthTokenDetails
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("expires_in")]
        public int ExpiresInSec { get; set; }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        //[JsonConverter(typeof(SpaceDelimitedListConverter))]
        [JsonProperty("scope")]
        //public IEnumerable<string> Scopes {get; set; }
        public string Scopes { get; set; }
    }


    //public class SpaceDelimitedListConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return objectType == typeof(List<string>);
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        var listValue = value as IEnumerable<string> ?? Enumerable.Empty<string>();
    //        var converted = string.Join(" ", listValue);
    //        writer.WriteValue(converted);
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        var stringValue = reader.Value as string;
    //        var converted = stringValue?.Split(' ') ?? Enumerable.Empty<string>();
    //        return new List<string>(converted);
    //    }
    //}
}
