/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
import * as cdk from 'aws-cdk-lib';
import { CfnOutput } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import { CognitoStack } from './cognito-stack';
import { WebSocketApiStack } from './websocket-api-stack';
import { ConfigStack } from './config-stack';
import { CfnUserPoolUser } from 'aws-cdk-lib/aws-cognito'
import { NagSuppressions } from 'cdk-nag';

export class WebSocketDemoStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    //Get Administrator Email Address as a CloudFormation Parameter
    const AdminEmail = new cdk.CfnParameter(this, 'AdminEmailAddress', {
      type: 'String',
      default: 'admin email address'
    });

    // Create Cognito User Pool
    const cognitoStack = new CognitoStack(this, 'CognitoStack', {});

    // Create a UserPool user for the administrator
    new CfnUserPoolUser(this, "AdminUser", {
      username: AdminEmail.valueAsString,
      userPoolId: cognitoStack.UserPoolId,
      desiredDeliveryMediums: ['EMAIL'],
      userAttributes: [{
        name: 'email',
        value: AdminEmail.valueAsString
      }]
    });

    // Create API Gateway WebSocket API
    const webSocketApiStack = new WebSocketApiStack(this, 'WebSocketApiStack', {
      parentStackName: this.stackName,
      stackName: 'WebSocketApiStack',
      stageName: 'prod'
    });

    // Create Config Bucket and JSON file
    const configStack = new ConfigStack(this, 'ConfigStack', {
      cognitoStack: cognitoStack,
      webSocketApiStack: webSocketApiStack
    });

    // Outputs
    new CfnOutput(this, "User-Pool-Id", {
      value: cognitoStack.UserPoolId,
    });

    new CfnOutput(this, "App-Client-Id", {
      value: cognitoStack.AppClientId
    });

    new CfnOutput(this, "WebSocket-API-URL", {
      value: webSocketApiStack.WebSocketApi.apiEndpoint + '/prod'
    });
  }
}
