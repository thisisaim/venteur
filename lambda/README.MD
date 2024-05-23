# CreateRequestHandler Lambda Function

## Overview

The `CreateRequestHandler` Lambda function is designed to facilitate the asynchronous processing of chess game requests where users query the shortest path a knight can take between two points on a chessboard. This document outlines the design rationale and key components of the Lambda function.

## Architecture

The function integrates with several AWS services, including Amazon DynamoDB for data persistence and Amazon SQS for decoupling and asynchronous message processing. The choice of these services was driven by the need for scalability, reliability, and manageability.

### Components

- **Amazon DynamoDB**: Used for storing and retrieving the details of each chess move request. DynamoDB offers fast, consistent performance and seamless scalability.
- **Amazon SQS**: Utilized for queuing the operation ID after a request is saved to DynamoDB, which triggers another process (potentially another Lambda) to calculate the knight's shortest path asynchronously.
- **Environment Variables**: Uses an environment variable `QUEUE_URL` to dynamically set the SQS queue URL, enhancing flexibility and configuration management.

### Error Handling

- The function includes robust error handling to manage and log exceptions that may occur during the execution, particularly with database operations and message queueing.

## Functions

### `CreateRequestAsync`

- Validates the input chess positions to ensure they are in the correct format.
- Generates a unique operation ID for each request.
- Saves the request details to DynamoDB.
- Sends a message to an SQS queue with the operation ID for further processing.

### `SaveRequestAsync`

- Handles interactions with DynamoDB to save request data.

### `SendMessageToQueue`

- Handles the sending of messages to the SQS queue.

### `IsValidChessPosition`

- Validates the format of the chess position provided by the user.

## Thought Process

### Scalability and Performance

- **DynamoDB** was chosen for its ability to handle high throughput and low latency, which is crucial for the responsive performance of the function.
- **SQS** helps in decoupling components of the system, allowing the path calculation process to scale independently based on demand.

### Reliability

- Error handling and logging mechanisms are in place to ensure that any failures in the process do not go unnoticed and can be addressed promptly.

### Manageability

- Using AWS managed services like DynamoDB and SQS reduces the overhead of infrastructure management, allowing more focus on feature development and optimization.

### Security

- The function uses IAM roles and policies to securely access AWS services, ensuring that it operates with the least privilege necessary to perform its tasks.

## Conclusion

The `CreateRequestHandler` is a critical component of a serverless architecture designed to handle chess game requests efficiently and reliably. By leveraging AWS services, it achieves high scalability, performance, and ease of management, making it well-suited for processing gaming moves or similar tasks in a cost-effective manner.



# PathCalculationHandler Lambda Function

## Overview

The `PathCalculationHandler` is an AWS Lambda function designed to asynchronously compute the shortest path a knight can take on a chessboard from a given start to an end position. This document describes the function's design and architecture, focusing on its integration with AWS services and the decision-making process behind its implementation.

## Architecture

This function is part of a serverless architecture that utilizes Amazon DynamoDB for storing requests and Amazon SQS for message-driven processing. The architecture is designed to handle requests asynchronously, improving scalability and responsiveness.

### Components

- **Amazon DynamoDB**: Used to store and retrieve the details of chess move requests. It provides fast access and strong consistency, essential for retrieving and updating request statuses effectively.
- **Amazon SQS**: Facilitates decoupled communication between this function and other components that enqueue messages after initiating requests. This separation allows independent scaling of request handling and path calculation components.
- **Lambda Function**: Acts as the processing unit that receives a message from SQS (containing an operation ID), fetches the corresponding request from DynamoDB, calculates the shortest path, updates the request with the result, and logs the completion.

### Error Handling

- Comprehensive error handling is implemented to manage exceptions during database operations, message processing, and path calculations. This ensures that all failures are logged and can be addressed without disrupting the user experience.

## Functions

### `FunctionHandler`

- Triggered by messages arriving in the SQS queue, this function orchestrates the retrieval of operation IDs and the processing of corresponding requests.

### `ProcessRequestAsync`

- Retrieves the request details from DynamoDB, calculates the path using the chess knight's movement rules, updates the request with the results, and saves the updates back to DynamoDB.

### `CalculateKnightPath`

- Implements the logic to calculate the shortest path using Breadth-First Search (BFS), considering the knight's unique movement abilities in chess.

## Thought Process

### Scalability and Responsiveness

- **DynamoDB** and **SQS** are used to ensure that the system can handle high volumes of requests and that the path calculation can scale independently based on demand.

### Reliability and Manageability

- Using managed services like DynamoDB and SQS reduces the overhead of managing infrastructure and ensures high availability and durability.

### Readability Over Complexity

- Instead of using complex regex patterns to validate chess positions, the function uses a more straightforward, readable method to check validity. This approach improves maintainability and reduces the potential for bugs that complex regex might introduce.

## Conclusion

The `PathCalculationHandler` Lambda function is a key component of a serverless architecture designed to efficiently process chess move calculations. By leveraging AWS services, it achieves excellent scalability, performance, and ease of management, making it well-suited for processing tasks that require asynchronous handling and computational logic.




# GetResultHandler Lambda Function

## Overview

The `GetResultHandler` Lambda function is designed to retrieve the results of a chess knight's shortest path calculation from a DynamoDB database, with caching implemented using SQLite to substitute for Redis. This document details the function's design and the rationale behind its implementation.

## Architecture

This function leverages Amazon DynamoDB for durable storage and retrieval of request details, and SQLite as a local caching mechanism to optimize response times and reduce database load.

### Components

- **Amazon DynamoDB**: Used for persistently storing request details, including the status and result of each path calculation.
- **SQLite Database**: Employed as a lightweight, file-based caching system to store results temporarily and reduce latency and DynamoDB query costs for frequently accessed data.

### Error Handling

- Robust error handling ensures that any exceptions during database access or cache operations are logged and managed, providing stability and reliability.

## Functions

### `FunctionHandler`

- Serves as the entry point for API Gateway requests. It extracts the operation ID, checks the cache, retrieves data from DynamoDB if necessary, and returns the result to the client.

### `CacheResult`

- Implements caching by storing results in an SQLite database, which acts as a low-cost, simple alternative to Redis.

### `GetCachedResult`

- Retrieves results from the SQLite cache if available, which can significantly speed up response times by avoiding database access.

## Thought Process

### Cost-Effectiveness

- **SQLite** replaces Redis to avoid the costs associated with AWS ElastiCache, particularly for a non-production, testing environment. SQLite provides sufficient performance for the expected load at minimal cost.

### Scalability and Simplicity

- While SQLite does not offer the same scalability as Redis, it is adequate for demonstration purposes and simple testing scenarios, significantly simplifying the architecture without incurring additional costs.

### Reliability and Performance

- Caching results reduce the number of read requests to DynamoDB, lowering costs and improving response times.

### Security and Manageability

- Using AWS Lambda and DynamoDB allows the function to scale automatically while maintaining security through AWS IAM roles and policies.

## Conclusion

The `GetResultHandler` Lambda function demonstrates a cost-effective, scalable, and reliable method for managing chess game calculations in a serverless environment. By using SQLite for caching, it achieves enhanced performance for repeated queries while controlling costs, making it an ideal solution for testing and development phases.


# How to Use

This document describes how to interact with the deployed AWS Lambda functions using the provided endpoints.

## 1. CreateRequestHandler

### Endpoint
https://5oyqu9jirk.execute-api.us-east-2.amazonaws.com/test/requesthandler


### Description
Initiates a new request to calculate the shortest path a knight can take between two points on a chessboard.

### Usage
- **Method:** POST
- **Headers:**
  - Content-Type: application/json
- **Body:**
  - JSON object containing the `source` and `target` chessboard positions.

```json
{
  "source": "A1",
  "target": "B3"
}
```
Response
Returns an operation ID which can be used to retrieve the calculation result.
```
{
  "operationId": "some-generated-id"
}
```
## 2. GetResultHandler

### Endpoint
https://5oyqu9jirk.execute-api.us-east-2.amazonaws.com/test/getresulthandler

### Description
Retrieves the result of a previously initiated request to calculate the shortest path a knight can take on a chessboard.

### Usage
- **Method:** GET
- **Headers:**
  - Content-Type: application/json
- **Query Parameters:**
  - operationId: The unique ID of the operation to retrieve the result for.

GET /getresulthandler?operationId=some-generated-id

Response
Returns the result of the path calculation.

Example Request URL:
```json
{
  "operationId": "some-generated-id",
  "source": "A1",
  "target": "B3",
  "result": ["A1", "C2", "B3"]
}
```
Response
Returns the calculation result, including the path taken and the number of moves.

```json
{
  "OperationId": "3bb51a71-fc9d-499a-88e6-e571713d767a",
  "Source": "A1",
  "Target": "B3",
  "Path": ["A1", "B3"],
  "NumberOfMoves": 1,
  "Message": "Result: Path - A1, B3, Moves - 1"
}
```