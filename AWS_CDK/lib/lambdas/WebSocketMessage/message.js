/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
import { DynamoDBClient } from "@aws-sdk/client-dynamodb";
import { DynamoDBDocumentClient, ScanCommand } from "@aws-sdk/lib-dynamodb";

import { ApiGatewayManagementApiClient, PostToConnectionCommand } from "@aws-sdk/client-apigatewaymanagementapi";

const dbClient = new DynamoDBClient({});
const dynamo = DynamoDBDocumentClient.from(dbClient);
const connectionsTableName = process.env.CONNECTIONS_TABLE;

async function getConnections() {
    const scanParams = {
        ExpressionAttributeNames: {
            "#C": "connectionId"
        },
        ProjectionExpression: "#C",
        TableName: connectionsTableName
    };

    try {
        const scanData = await dynamo.send(new ScanCommand(scanParams));

        if (scanData.Items == undefined) {
            throw new Error("Connections not found.");
        }
        return scanData.Items;

    } catch (err) {
        throw err;
    }
}

async function postToConnection(data, connectionId, apiClient) {
    if (typeof data !== "string") {
        data = JSON.stringify(data);
    }
    try {
        const input = {
            Data: data,
            ConnectionId: connectionId
        };

        const response = await apiClient.send(new PostToConnectionCommand(input));

    } catch (err) {
        throw err;
    }
}

export const handler = async (event, context) => {

    const apiClient = new ApiGatewayManagementApiClient({
        apiVersion: '2018-11-29',
        endpoint: `https://${event.requestContext.domainName}/${event.requestContext.stage}`,
    });

    const body = JSON.parse(event.body);
    const senderConnectionId = event.requestContext.connectionId;

    try {

        let recipients = [];

        const connections = await getConnections();

        connections.forEach(connection => {
            if (connection.connectionId != senderConnectionId) {
                recipients.push(connection.connectionId);
            }
        });

        // Post message to recipients
        for (const recipient of recipients) {
            await postToConnection(body.message, recipient, apiClient);
        }

    } catch (err) {
        console.error("Error sending message: " + err);
        return {
            statusCode: 500,
            body: "Error sending message."
        };
    }
    return {
        statusCode: 200,
    };
};