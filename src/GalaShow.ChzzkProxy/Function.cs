using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Configuration;
using GalaShow.Common.Cors;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.ChzzkProxy;

public sealed class Function
{
    // Never follow a redirect carrying an app secret or user token to another host.
    private static readonly HttpClient HttpClient = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private static readonly ChzzkCredentialsProvider Credentials = new();
    private readonly ChzzkIntegrationService _service;

    public Function() : this(new ChzzkIntegrationService(HttpClient, Credentials.GetAsync)) { }
    public Function(ChzzkIntegrationService service) => _service = service;

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        StageResolver.Resolve(request);
        await CorsHandler.InitializeAsync();
        if (request.HttpMethod == "OPTIONS")
            return CorsHandler.AddCorsHeaders(request, ChzzkIntegrationService.Json(200, "{\"code\":200,\"message\":null}"));

        APIGatewayProxyResponse response;
        try { response = await _service.HandleAsync(request); }
        catch (Exception)
        {
            // Requests, response bodies and exception messages can contain OAuth credentials.
            context?.Logger.LogError("CHZZK integration encountered an unexpected error.");
            response = ChzzkIntegrationService.Error(500, "INTERNAL_ERROR");
        }
        return CorsHandler.AddCorsHeaders(request, response);
    }
}
