/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
import { CognitoJwtVerifier } from 'aws-jwt-verify';

export const handler = async (event, context) => {
    // Expected query string parameters
    const userPoolId = event.queryStringParameters.userPoolId;
    const clientId = event.queryStringParameters.clientId;
    const token = event.queryStringParameters.Authorization;

    // Verifier that expects valid access tokens:
    const verifier = CognitoJwtVerifier.create({
        userPoolId: userPoolId,
        tokenUse: "access",
        clientId: clientId,
        includeRawJwtInErrors: true
    });

    try {
        const payload = await verifier.verify(token);
        console.log("Token is valid. Payload:", payload);
    } catch (err) {
        console.log("Token not valid!");
        return {
            statusCode: 401,
            body: JSON.stringify('Unauthorized.')
        };
    }
    
    return generateAllow('me', event.methodArn);
};

// Helper function to generate an IAM policy
var generatePolicy = function(principalId, effect, resource) {
    // Required output:
    var authResponse = {};
     authResponse.principalId = principalId;
    if (effect && resource) {
        var policyDocument = {};
         policyDocument.Version = '2012-10-17'; // default version
        policyDocument.Statement = [];
        var statementOne = {};
         statementOne.Action = 'execute-api:Invoke'; // default action
        statementOne.Effect = effect;
         statementOne.Resource = resource;
         policyDocument.Statement[0] = statementOne;
         authResponse.policyDocument = policyDocument;
     }
    return authResponse;
 }
     
 var generateAllow = function(principalId, resource) {
    return generatePolicy(principalId, 'Allow', resource);
 }