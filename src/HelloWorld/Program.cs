using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.APIGatewayEvents;

namespace HelloWorld
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> handler =
                new Function().FunctionHandler;

            using var bootstrap = LambdaBootstrapBuilder
                .Create<APIGatewayProxyRequest, APIGatewayProxyResponse>(
                    handler,
                    new DefaultLambdaJsonSerializer()
                )
                .Build();

            await bootstrap.RunAsync();
        }
    }
}