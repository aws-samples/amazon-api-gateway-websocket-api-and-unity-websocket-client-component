# A Unity WebSocket client component for connecting to an Amazon API Gateway WebSocket API

A simple game that authenticates the player with Amazon Cognito, and then pits them in a game of tag with other players. The game networking is handled through Amazon API Gateway WebSocket API messages.

<img src="../../docs/img/WebSocket-Tag-low.gif?raw=true" alt="Animated GIF of two WebSocket Tag game instances. A player on each instance is moving around, while their movement is shown on the opposite instance."> <br />
*The left and right sides are separate instances of the WebSocket Tag game, each connected to the API Gateway WebSocket API. The player actions are passed via WebSocket messages.*

## Prerequisites
* [Unity Engine 2021.3.19f1+](https://unity.com/download)
* [Backend Setup](../../AWS_CDK/README.md)
* `aws-config.json` from the backend setup.

## Setup Instructions
1. Open the project in Unity3D.
2. Copy the `aws-config.json` file from the deployed Amazon S3 bucket `configBucket` to your [Unity persistent data path (OS dependent)](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html).<br />
For Windows: ```%userprofile%\AppData\LocalLow\DefaultCompany\aws-websocket-api-demo``` <br />
For Mac: ```~/Library/Application Support/DefaultCompany/aws-websocket-api-demo```
3. Press Play to start the application. Alternatively, you can [build a standalone player](https://docs.unity3d.com/Manual/BuildSettings.html).

## Game Instructions
1. Ensure the `aws-config.json` file is in the persistent data path as listed in the Setup Instructions.
2. Launch the game.
3. Enter your Amazon Cognito user name and password, then click the Login button.
4. Enter a player name, then click the Join Game button.
5. The goal is to find and tag other players. You will see `?` clues when other players move. If they try to tag you, you will see a `!` symbol.
6. To move, use the `WASD` or `Arrow` keys. To tag other players, use the `Spacebar` or `Keypad0` key.
7. Good luck!
