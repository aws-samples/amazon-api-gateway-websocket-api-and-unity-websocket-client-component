/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */

import * as cdk from 'aws-cdk-lib';
import { RemovalPolicy } from 'aws-cdk-lib';
import { Construct } from 'constructs';

import * as cognito from 'aws-cdk-lib/aws-cognito'
import { NagSuppressions } from 'cdk-nag';

export class CognitoStack extends cdk.NestedStack {
    public AWS_Region: string;
    public UserPool: cognito.UserPool;
    public UserPoolId: string;
    public AppClient: cognito.UserPoolClient;
    public AppClientId: string;
    public IdentityPool: cognito.CfnIdentityPool;
    public IdentityPoolId: string;

    constructor(scope: Construct, id: string, props?: cdk.StackProps) {
        super(scope, id, props);
        this.AWS_Region = this.region;
        this.UserPool = new cognito.UserPool(this, 'websocket-api-demo-user-pool', {
            userPoolName: 'websocket-api-demo-user-pool',
            selfSignUpEnabled: false,
            removalPolicy: RemovalPolicy.DESTROY,
            passwordPolicy: {
                minLength: 8,
                requireDigits: true,
                requireLowercase: true,
                requireSymbols: true,
                requireUppercase: true
            }
        });
        this.UserPoolId = this.UserPool.userPoolId;
        this.AppClient = this.UserPool.addClient('unity-client', {
            supportedIdentityProviders: [
                cognito.UserPoolClientIdentityProvider.COGNITO
            ],
            authFlows: {
                userPassword: true,
                userSrp: true
            }
        });
        this.AppClientId = this.AppClient.userPoolClientId;
        this.IdentityPool = new cognito.CfnIdentityPool(this, id + "IdentityPool", {
            allowUnauthenticatedIdentities: false,
            cognitoIdentityProviders: [
                {
                    clientId: this.AppClientId,
                    providerName: this.UserPool.userPoolProviderName
                }
            ]
        });
        this.IdentityPoolId = this.IdentityPool.ref;

        // CDK Nag Suppressions

        NagSuppressions.addResourceSuppressions(this.UserPool, [
            {
                id: 'AwsSolutions-COG2',
                reason: 'MFA should be added in a production environment.'
            },
            {
                id: 'AwsSolutions-COG3',
                reason: 'AdvancedSecurityMode is deprecated.'
            }
        ]);

    }

    exportConfig() {
        const config = {
            'AWS_Region': this.AWS_Region,
            'CognitoUserPool': this.UserPool.userPoolId,
            'CognitoIdentityPool': this.IdentityPoolId,
            'AppClientId': this.AppClient.userPoolClientId
        }
        return config;
    }

}