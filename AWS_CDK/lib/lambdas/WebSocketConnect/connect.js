/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
import { DynamoDBClient } from "@aws-sdk/client-dynamodb";
import { DynamoDBDocumentClient, PutCommand} from "@aws-sdk/lib-dynamodb";

const dbClient = new DynamoDBClient({});
const dynamo = DynamoDBDocumentClient.from(dbClient);

const connectionsTable = process.env.CONNECTIONS_TABLE;

export const handler = async (event, context) => {
    const params = {
        TableName : connectionsTable,
        Item: {
            connectionId: event.requestContext.connectionId,
            expiryTime: Date.now() * 0.001 + 86400 // expire in 24 hours
        }
    };
  
    try {
        console.log("Storing connection id " + event.requestContext.connectionId + " ...");
        await dynamo.send(new PutCommand(params));
        console.log("Successfully stored connection id");
    } catch (err) {
        console.error("Error storing connection: " + err);
        return {
            statusCode: 500,
            body: "Error storing connectionId"
        };
    }
    return {
        statusCode: 200
    };
};