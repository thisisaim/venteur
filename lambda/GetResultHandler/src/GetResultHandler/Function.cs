using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2.DataModel;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ChessLambda
{
    public class GetResultHandler
    {
        private static readonly AmazonDynamoDBClient _dynamoDbClient = new AmazonDynamoDBClient();
        private static readonly DynamoDBContext _dbContext = new DynamoDBContext(_dynamoDbClient);
        private static Dictionary<string, string> _cache = new Dictionary<string, string>();

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("FunctionHandler started");
            context.Logger.LogLine("Received request: " + JsonConvert.SerializeObject(request));

            if (request.QueryStringParameters == null || !request.QueryStringParameters.TryGetValue("operationId", out string operationId) || string.IsNullOrEmpty(operationId))
            {
                return new APIGatewayProxyResponse { StatusCode = 400, Body = "Operation ID is required" };
            }

            if (_cache.TryGetValue(operationId, out string cachedResponse))
            {
                return new APIGatewayProxyResponse { StatusCode = 200, Body = cachedResponse };
            }

            var requestItem = await _dbContext.LoadAsync<RequestItem>(operationId);
            if (requestItem == null)
            {
                return new APIGatewayProxyResponse { StatusCode = 404, Body = "Operation ID not found" };
            }

            if (requestItem.Status != "Completed")
            {
                return new APIGatewayProxyResponse { StatusCode = 200, Body = "Calculation is still processing" };
            }

            var result = new
            {
                OperationId = requestItem.OperationId,
                Source = requestItem.Source,
                Target = requestItem.Target,
                Path = requestItem.Path,
                NumberOfMoves = requestItem.NumberOfMoves
            };

            string responseBody = JsonConvert.SerializeObject(result);
            _cache[operationId] = responseBody;

            return new APIGatewayProxyResponse { StatusCode = 200, Body = responseBody };
        }
    }

    [DynamoDBTable("KnightPathRequests")]
    public class RequestItem
    {
        [DynamoDBHashKey]
        public string OperationId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> Path { get; set; } = new List<string>();
        public int NumberOfMoves { get; set; } = 0;
    }
}
