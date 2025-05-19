/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */

import * as cdk from 'aws-cdk-lib';
import { Duration } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as apigatewayv2 from 'aws-cdk-lib/aws-apigatewayv2'
import { WebSocketLambdaIntegration } from 'aws-cdk-lib/aws-apigatewayv2-integrations'
import { WebSocketLambdaAuthorizer } from 'aws-cdk-lib/aws-apigatewayv2-authorizers'
import * as lambda from 'aws-cdk-lib/aws-lambda'
import * as ddb from 'aws-cdk-lib/aws-dynamodb'
import * as logs from 'aws-cdk-lib/aws-logs';
import { NagSuppressions } from 'cdk-nag';

interface WebSocketApiStackProps extends cdk.NestedStackProps {
    parentStackName: string
    stackName: string
    stageName: string
}

export class WebSocketApiStack extends cdk.NestedStack {
    public AWS_Region: string;
    public WebSocketApi: apigatewayv2.WebSocketApi
    StageName: string;

    constructor(scope: Construct, id: string, props: WebSocketApiStackProps) {
        super(scope, id, props);

        this.AWS_Region = this.region;
        this.StageName = props.stageName;

        // DynamoDB tables for WebSocket API
        const connectionsTable = new ddb.Table(this, 'websocket-api-demo-connections-table', {
            partitionKey: {
                name: 'connectionId',
                type: ddb.AttributeType.STRING
            },
            removalPolicy: cdk.RemovalPolicy.DESTROY,
            timeToLiveAttribute: "expiryTime"
        });

        // $connect route for WebSocket API 
        const connectLambda = new lambda.Function(this, 'ws-connect-lambda', {
            runtime: lambda.Runtime.NODEJS_22_X,
            handler: 'connect.handler',
            code: lambda.Code.fromAsset('lib/lambdas/WebSocketConnect'),
            timeout: Duration.seconds(30),
            environment: {
                "CONNECTIONS_TABLE": connectionsTable.tableName
            }
        });
        connectionsTable.grantReadWriteData(connectLambda);

        // $disconnect route for WebSocket API 
        const disconnectLambda = new lambda.Function(this, 'ws-disconnect-lambda', {
            runtime: lambda.Runtime.NODEJS_22_X,
            handler: 'disconnect.handler',
            code: lambda.Code.fromAsset('lib/lambdas/WebSocketDisconnect'),
            timeout: Duration.seconds(30),
            environment: {
                "CONNECTIONS_TABLE": connectionsTable.tableName
            }
        });
        connectionsTable.grantReadWriteData(disconnectLambda);

        // message route for WebSocket API 
        const messageLambda = new lambda.Function(this, 'ws-message-lambda', {
            runtime: lambda.Runtime.NODEJS_22_X,
            handler: 'message.handler',
            code: lambda.Code.fromAsset('lib/lambdas/WebSocketMessage'),
            timeout: Duration.seconds(30),
            environment: {
                "CONNECTIONS_TABLE": connectionsTable.tableName
            }
        });
        connectionsTable.grantReadData(messageLambda);

        const heartbeatLambda = new lambda.Function(this, 'ws-heartbeat-lambda', {
            runtime: lambda.Runtime.NODEJS_22_X,
            handler: 'heartbeat.handler',
            code: lambda.Code.fromAsset('lib/lambdas/WebSocketHeartbeat'),
            timeout: Duration.seconds(30)
        })

        // WebSocket API Authorizer Lambda
        const authLambda = new lambda.Function(this, 'ws-authorizer-lambda', {
            runtime: lambda.Runtime.NODEJS_22_X,
            handler: 'authWebSocket.handler',
            code: lambda.Code.fromAsset('lib/lambdas/WebSocketAuth'),
            timeout: Duration.seconds(30)
        })

        // WebSocket API

        const webSocketApi = new apigatewayv2.WebSocketApi(this, 'websocket-api-demo', {
            apiName: 'websocket-api',
            connectRouteOptions: {
                integration: new WebSocketLambdaIntegration('ConnectIntegration', connectLambda),
                authorizer: new WebSocketLambdaAuthorizer('WebSocketAuthorizer', authLambda, { identitySource: ['route.request.querystring.Authorization'] })
            },
            disconnectRouteOptions: {
                integration: new WebSocketLambdaIntegration('DisconnectIntegration', disconnectLambda)
            }
        });
        this.WebSocketApi = webSocketApi;

        // Add WebSocket API route Lambda integrations

        webSocketApi.addRoute('message', {
            integration: new WebSocketLambdaIntegration('MessageIntegration', messageLambda),
        });

        webSocketApi.addRoute('heartbeat', {
            integration: new WebSocketLambdaIntegration('HeartbeatIntegration', heartbeatLambda)
        });

        // Add connection management permissions to message lambda
        webSocketApi.grantManageConnections(messageLambda);

        // Add WebSocket API Stage
        const webSocketApiAccessLogs = new logs.LogGroup(this, "WebSocketApiAccessLogs", {
            retention: logs.RetentionDays.ONE_WEEK,
            removalPolicy: cdk.RemovalPolicy.DESTROY,
        });

        new apigatewayv2.CfnStage(this, `websocket-${props.stageName}-stage`, {
            apiId: webSocketApi.apiId,
            stageName: props.stageName,
            accessLogSettings: {
                destinationArn: webSocketApiAccessLogs.logGroupArn,
                format: JSON.stringify({
                    "requestId": "$context.requestId",
                    "ip": "$context.identity.sourceIp",
                    "caller": "$context.identity.caller",
                    "user": "$context.identity.user",
                    "requestTime": "$context.requestTime",
                    "httpMethod": "$context.httpMethod",
                    "resourcePath": "$context.resourcePath",
                    "status": "$context.status",
                    "protocol": "$context.protocol",
                    "responseLength": "$context.responseLength"
                })
            },
            autoDeploy: true,
            defaultRouteSettings: {
                dataTraceEnabled: true,
                detailedMetricsEnabled: true,
                loggingLevel: "INFO",
            }
        });

        // CDK Nag Suppressions

        NagSuppressions.addResourceSuppressions(connectionsTable, [
            {
                id: 'AwsSolutions-DDB3',
                reason: 'Point in time recovery not needed for the connections table.'
            }
        ])

        NagSuppressions.addResourceSuppressions(
            this,
            [
                {
                    id: 'AwsSolutions-IAM4',
                    reason: 'Intend to use AWSLambdaBasicExecutionRole for this project',
                    appliesTo: [
                        {
                            regex: "/^.*AWSLambdaBasicExecutionRole$/g"
                        }
                    ]
                },
                {
                    id: 'AwsSolutions-IAM5',
                    reason: "Intend to use default policy for this project.",
                    appliesTo: [
                        {
                            regex: `/^Resource::arn:aws:execute-api:${this.region}:${this.account}:<websocketapidemo.*>/\\*/\\*/@connections/\\*$/g`
                        }
                    ]
                }
            ],
            true
        );

        NagSuppressions.addResourceSuppressionsByPath(
            this,
            [
                `/${props.parentStackName}/${props.stackName}/websocket-api-demo/$disconnect-Route/Resource`,
                `/${props.parentStackName}/${props.stackName}/websocket-api-demo/message-Route/Resource`,
                `/${props.parentStackName}/${props.stackName}/websocket-api-demo/heartbeat-Route/Resource`
            ],
            [
                {
                    id: "AwsSolutions-APIG4",
                    reason: "This route doesn't need authorization because it is only accessible after connecting via the $connect route, which has authorization."
                }
            ],
            true
        );
    }


    exportConfig() {
        const config = {
            'AWS_Region': this.AWS_Region,
            'WebSocket_API_URL': `${this.WebSocketApi.apiEndpoint}/${this.StageName}`
        }
        return config;
    }
}