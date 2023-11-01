/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
import * as cdk from 'aws-cdk-lib';
import { Duration, RemovalPolicy } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as lambda from 'aws-cdk-lib/aws-lambda'
import * as s3 from 'aws-cdk-lib/aws-s3'
import * as iam from 'aws-cdk-lib/aws-iam'
import * as cr from 'aws-cdk-lib/custom-resources';
import { CustomResource } from 'aws-cdk-lib';
import { NagSuppressions } from 'cdk-nag';
import { CognitoStack } from './cognito-stack';
import { WebSocketApiStack } from './websocket-api-stack';

interface ConfigStackProps extends cdk.NestedStackProps {
  cognitoStack: CognitoStack;
  webSocketApiStack: WebSocketApiStack;
}

export class ConfigStack extends cdk.NestedStack {
  constructor(scope: Construct, id: string, props: ConfigStackProps) {
    super(scope, id, props);

    const accessLogsBucket = new s3.Bucket(this, "accessLogsBucket", {
      autoDeleteObjects: true,
      removalPolicy: RemovalPolicy.DESTROY,
      enforceSSL: true
    });

    const configBucket = new s3.Bucket(this, "configBucket", {
      autoDeleteObjects: true,
      removalPolicy: RemovalPolicy.DESTROY,
      enforceSSL: true,
      serverAccessLogsBucket: accessLogsBucket
    });
    const configFunction = new lambda.Function(this, 'config-lambda', {
      runtime: lambda.Runtime.NODEJS_18_X,
      handler: 'configFile.handler',
      code: lambda.Code.fromAsset('lib/lambdas/ConfigFile'),
      timeout: Duration.seconds(30),
      environment: {
        'DEPLOYMENT_BUCKET_NAME': configBucket.bucketName
      }
    });

    const s3PutObjectPolicy = new iam.PolicyStatement({
      actions: ['s3:PutObject'],
      resources: [`${configBucket.bucketArn}/*`] // TODO: Refine policy if possible to exclude wildcard
    })

    configFunction.role?.attachInlinePolicy(
      new iam.Policy(this, 'put-object-policy', {
        statements: [s3PutObjectPolicy]
      })
    );

    const crProvider = new cr.Provider(this, "config-resource-provider", {
      onEventHandler: configFunction
    })

    const configResource = new CustomResource(this, "config-resource", {
      serviceToken: crProvider.serviceToken,
      properties: {
        "configs": {
          'Cognito': props.cognitoStack.exportConfig(),
          'API_Gateway': props.webSocketApiStack.exportConfig()
        }
      }
    });

    // CDK Nag Suppressions
    NagSuppressions.addResourceSuppressions(
      this,
      [
        {
          id: "AwsSolutions-IAM4",
          reason: "Intend to use AWSLambdaBasicExecutionRole as is at this stage of this project.",
          appliesTo: [
            {
              regex: "/^.*AWSLambdaBasicExecutionRole$/g",
            },
          ],
        },
        {
          id: "AwsSolutions-IAM5",
          reason: "The Config Lambda function requires access to objects in the configBucket",
          appliesTo: [
            {
              regex: "/^Resource::<configBucket.*\\.Arn>/\\*$/g"
            },
          ],
        },
        {
          id: "AwsSolutions-IAM5",
          reason: "Intend to use AWSLambdaBasicExecutionRole as is at this stage of this project.",
          appliesTo: [
            {
              regex: "/^Resource::<configlambda.*\\.Arn>:\\*$/g"
            },
          ],
        },
      ],
      true
    );

  }
}
