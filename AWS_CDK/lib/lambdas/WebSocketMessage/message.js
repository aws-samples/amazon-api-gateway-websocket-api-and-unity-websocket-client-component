/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
import { DynamoDBClient } from "@aws-sdk/client-dynamodb";
import { DynamoDBDocumentClient, ScanCommand, DeleteCommand } from "@aws-sdk/lib-dynamodb";

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

async function deleteConnection(connectionId) {
    const params = {
        TableName: connectionsTableName,
        Key: {
            connectionId: connectionId
        }
    };

    try {
        await dynamo.send(new DeleteCommand(params));
        console.log(`Removed stale connection: ${connectionId}`);
    } catch (err) {
        console.error(`Error removing stale connection ${connectionId}:`, err);
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
        return true; // Success

    } catch (err) {
        // Check if the error is due to a stale/invalid connection
        if (err.statusCode === 410 || 
            err.name === 'GoneException' || 
            err.name === 'BadRequestException' ||
            (err.message && err.message.includes('Invalid connectionId'))) {
            console.log(`Connection ${connectionId} is invalid/stale, removing from database. Error: ${err.message}`);
            await deleteConnection(connectionId);
            return false; // Connection was stale/invalid
        }
        // Re-throw other errors
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
        let successCount = 0;
        let staleCount = 0;
        
        for (const recipient of recipients) {
            const success = await postToConnection(body.message, recipient, apiClient);
            if (success) {
                successCount++;
            } else {
                staleCount++;
            }
        }
        
        console.log(`Message sent to ${successCount} recipients, ${staleCount} stale connections removed`);

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