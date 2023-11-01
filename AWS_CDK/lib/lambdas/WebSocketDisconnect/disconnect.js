/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
import { DynamoDBClient } from "@aws-sdk/client-dynamodb";
import { DynamoDBDocumentClient, DeleteCommand } from "@aws-sdk/lib-dynamodb";

const dbClient = new DynamoDBClient({});
const dynamo = DynamoDBDocumentClient.from(dbClient);
const connectionsTableName = process.env.CONNECTIONS_TABLE;

async function deleteConnection(connectionId) {
  const params = {
    TableName: connectionsTableName,
    Key: {
      connectionId: connectionId
    }
  };

  try {
    await dynamo.send(new DeleteCommand(params));
  } catch (err) {
    throw err;
  }
}

export const handler = async (event, context) => {
  const connectionId = event.requestContext.connectionId;

  try {
    // Delete connection
    await deleteConnection(connectionId);
  }
  catch (err) {
    console.error("Error disconnecting: " + err);
    return {
      statusCode: 500,
      body: "Error disconnecting"
    };
  }
  return {
    statusCode: 200,
  };
};
