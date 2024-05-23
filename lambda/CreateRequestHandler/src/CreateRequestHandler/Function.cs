using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace chessLambda{
  public class CreateRequestHandler
  {
      private readonly IDynamoDBContext _dbContext;
      private readonly IAmazonSQS _sqsClient;
      private readonly string _queueUrl;

    public CreateRequestHandler()
      {
          var dynamoDbClient = new AmazonDynamoDBClient();
          _dbContext = new DynamoDBContext(dynamoDbClient);
          _sqsClient = new AmazonSQSClient();
          _queueUrl = Environment.GetEnvironmentVariable("QUEUE_URL") ?? throw new InvalidOperationException("QUEUE_URL environment variable is not set.");
      }


      public async Task<Response> CreateRequestAsync(Request input, ILambdaContext lambdaContext)
      {
          try
          {
              if (!IsValidChessPosition(input.Source) || !IsValidChessPosition(input.Target))
              {
                  return new Response
                  {
                      OperationId = null,
                      Message = "Invalid source or target position. Please use positions like 'A1', 'H8', etc."
                  };
              }

              string operationId = Guid.NewGuid().ToString();
              var requestItem = new RequestItem
              {
                  OperationId = operationId,
                  Source = input.Source,
                  Target = input.Target,
                  Status = "Processing"
              };

              await SaveRequestAsync(requestItem);
              await SendMessageToQueue(operationId);

              return new Response
              {
                  OperationId = operationId,
                  Message = "Operation Id created. Please query it to find your results."
              };
          }
          catch (Exception ex)
          {
              lambdaContext.Logger.LogLine($"Error processing request: {ex.Message}");
              return new Response
              {
                  OperationId = null,
                  Message = "An error occurred while processing your request."
              };
          }
      }

      private async Task SaveRequestAsync(RequestItem item)
      {
        try
        {
          await _dbContext.SaveAsync(item);
        }
        catch (Exception ex)
        {
          Serilog.Log.Error($"Error saving request: {ex.Message}");
          throw;
        }
      }

      private async Task SendMessageToQueue(string operationId)
      {
          var sendMessageRequest = new SendMessageRequest
          {
              QueueUrl = _queueUrl,
              MessageBody = operationId
          };

          try
          {
            await _sqsClient.SendMessageAsync(sendMessageRequest);
          }
          catch (Exception ex)
          {
            Serilog.Log.Error($"Error sending message to queue: {ex.Message}");
            throw;
          }
      }

      private bool IsValidChessPosition(string position)
      {
          if (string.IsNullOrEmpty(position) || position.Length != 2)
              return false;

          char file = position[0];
          char rank = position[1];

          return file >= 'A' && file <= 'H' && rank >= '1' && rank <= '8';
      }
  }
}

public class Request
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
}

public class Response
{
    public string? OperationId { get; set; }
    public string Message { get; set; } = string.Empty;
}

[DynamoDBTable("KnightPathRequests")]
public class RequestItem
{
    [DynamoDBHashKey]
    public string OperationId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<string>? Path { get; set; } = new List<string>();
    public int NumberOfMoves { get; set; } = 0;
}
