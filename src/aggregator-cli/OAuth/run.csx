#r "../bin/aggregator-function.dll"
#r "../bin/aggregator-shared.dll"

using System.Threading;

using aggregator;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ILogger logger, Microsoft.Azure.WebJobs.ExecutionContext context, CancellationToken cancellationToken)
{
    var handler = new AzureFunctionOAuthHandler(logger, context);
    var result = await handler.CallbackAsync(req, cancellationToken);
    return result;
}
