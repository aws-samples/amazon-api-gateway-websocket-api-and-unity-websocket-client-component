/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
import { S3Client, PutObjectCommand, DeleteObjectCommand } from "@aws-sdk/client-s3"

const s3 = new S3Client();
const bucketName = process.env.DEPLOYMENT_BUCKET_NAME;
const key = 'aws-config.json';

export const handler = async (event, context) => {

    try {
        console.log(event);
        const properties = event["ResourceProperties"]["configs"];
        const requestType = event["RequestType"].toLowerCase();

        if (requestType == 'create') {
            return await onCreate(properties);
        }

        if (requestType == 'update') {
            return await onUpdate(properties)
        }

        if (requestType == 'delete') {
            return await onDelete();
        }
    } catch (err) {
        console.error(err);
        return;
    }
}

const onCreate = async (properties) => {
    const putCommand = new PutObjectCommand({
        Body: JSON.stringify(properties),
        Bucket: bucketName,
        Key: key
    });
    await s3.send(putCommand);
    return;
}

const onUpdate = async (properties) => {
    const putCommand = new PutObjectCommand({
        Body: JSON.stringify(properties),
        Bucket: bucketName,
        Key: key
    });
    await s3.send(putCommand);
    return;
}

const onDelete = async () => {
    const deleteCommand = new DeleteObjectCommand({
        Bucket: bucketName,
        Key: key
    });
    await s3.send(deleteCommand);
    return;
}