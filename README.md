# Amazon API Gateway WebSocket API and Unity WebSocket client component

This sample project demonstrates deploying an Amazon API Gateway WebSocket API and building a Unity WebSocket Client component. 

## The project includes two parts:

1. AWS Cloud Development Kit (AWS CDK) project that deploys an authenticated Amazon API Gateway WebSocket API.

![Alt](./docs/img/websocket-api-architecture.jpg "Amazon API Gateway WebSocket API architecture diagram showing the AWS Lambda integrations and Amazon DynamoDB database.")<br />
*Reference architecture for the WebSocket API*

2. A Unity project that includes a WebSocket Client and an example of WebSocket message crafting and parsing for tag-like gameplay.

<img src="./docs/img/WebSocket-Tag-low.gif?raw=true" alt="Animated GIF of two WebSocket Tag game instances. A player on each instance is moving around, while their movement is shown on the opposite instance."> <br />
*The left and right sides are separate instances of the WebSocket Tag game, each connected to the API Gateway WebSocket API. The player actions are passed via WebSocket messages.*

## Follow steps in the following pages to setup the sample.
1. Start with setting up the WebSocket API infrastructure: [AWS Setup](./AWS_CDK/README.md)
2. Confirm you have the `aws-config.json` file from the configuration storage bucket. 
3. Lastly we will setup the application: [Unity Setup](./Unity3D/aws-websocket-api-demo/README.md)

## Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

## License

This library is licensed under the MIT-0 License. See the LICENSE file.
