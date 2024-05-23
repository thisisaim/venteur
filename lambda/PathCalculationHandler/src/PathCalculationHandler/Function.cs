using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace chessLambda
{
    public class PathCalculationHandler
    {
        private readonly IDynamoDBContext _dbContext;

        public PathCalculationHandler()
        {
            var dynamoDbClient = new AmazonDynamoDBClient();
            _dbContext = new DynamoDBContext(dynamoDbClient);
        }

        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            foreach (var record in evnt.Records)
            {
                var operationId = record.Body;
                await ProcessRequestAsync(operationId, context);
            }
        }

        private async Task ProcessRequestAsync(string operationId, ILambdaContext context)
        {
            var requestItem = await _dbContext.LoadAsync<RequestItem>(operationId);
            if (requestItem == null)
            {
                context.Logger.LogLine($"No item found with operation ID: {operationId}");
                return;
            }

            var pathResult = CalculateKnightPath(requestItem.Source, requestItem.Target);
            Console.WriteLine($"Path: {string.Join(", ", pathResult.Path)}, Moves: {pathResult.Moves}");
            requestItem.Path = pathResult.Path;
            requestItem.NumberOfMoves = pathResult.Moves;
            requestItem.Status = "Completed";

            await _dbContext.SaveAsync(requestItem);
            context.Logger.LogLine($"Path calculation completed for operation ID: {operationId}");
        }

        private PathResult CalculateKnightPath(string start, string end)
        {
            var directions = new List<(int, int)>
            {
                (2, 1), (2, -1), (-2, 1), (-2, -1),
                (1, 2), (1, -2), (-1, 2), (-1, -2)
            };

            var queue = new Queue<(string Position, List<string> Path)>();
            var visited = new HashSet<string>();
            queue.Enqueue((start, new List<string> { start }));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (currentPosition, path) = queue.Dequeue();
                if (currentPosition == end)
                {
                    return new PathResult { Path = path, Moves = path.Count - 1 };
                }

                int row = currentPosition[1] - '1';
                int col = currentPosition[0] - 'A';

                foreach (var (dr, dc) in directions)
                {
                    int newRow = row + dr;
                    int newCol = col + dc;
                    string newPosition = $"{(char)('A' + newCol)}{newRow + 1}";
                    if (IsValidPosition(newRow, newCol) && !visited.Contains(newPosition))
                    {
                        visited.Add(newPosition);
                        var newPath = new List<string>(path) { newPosition };
                        queue.Enqueue((newPosition, newPath));
                    }
                }
            }

            Console.WriteLine("No path found, it's possible that the target position is unreachable from the source position.");
            return new PathResult { Path = new List<string>(), Moves = 0 };
        }

        private bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < 8 && col >= 0 && col < 8;
        }
    }
}

public class PathResult
{
    public List<string> Path { get; set; } = new List<string>();
    public int Moves { get; set; } = 0;
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