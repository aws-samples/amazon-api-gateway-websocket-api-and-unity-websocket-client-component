/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amazon.Unity
{
    public class WebSocketDemoMessageParser : MonoBehaviour
    {
        public class Message
        {
            /// <summary>
            /// Message type used for websocket client business logic
            /// </summary>
            public string Type;

            /// <summary>
            /// Message content
            /// </summary>
            public string Content;

            public Message(string type, string content)
            {
                Type = type;
                Content = content;
            }
        }

        private GameBoard gameBoard;


        private void Awake()
        {
            gameBoard = GetComponent<GameBoard>();
        }

        private void Start()
        {
            AmazonAPIGatewayWebSocketManager.Instance.OnMessage += ParseMessage;
        }

        private void OnDestroy()
        {
            if (AmazonAPIGatewayWebSocketManager.Instance != null)
            {
                AmazonAPIGatewayWebSocketManager.Instance.OnMessage -= ParseMessage;
            }
        }

        private void ParseMessage(object sender, AmazonAPIGatewayWebSocketManager.MessageEventArgs messageArgs)
        {
            ParseMessage(messageArgs.Content);
        }

        private void ParseMessage(string jsonContent)
        {
            try
            {
                Message parsedMessage = JsonUtility.FromJson<Message>(jsonContent);

                if(parsedMessage != null)
                {
                    switch (parsedMessage.Type)
                    {
                        case "move":
                            // Highlight the ? tile
                            if (System.Int32.TryParse(parsedMessage.Content, out int pIndex))
                            {
                                gameBoard.TriggerPassiveDetection(pIndex);
                            }
                            break;
                        case "search":
                            // Highlight the ! tile
                            if (System.Int32.TryParse(parsedMessage.Content, out int aIndex))
                            {
                                gameBoard.TriggerActiveDetection(aIndex);
                            }
                            break;
                        case "playerFound":
                            // Show message that player was Found
                            gameBoard.PlayerTaggedMessage(parsedMessage.Content);
                            break;
                        case "playerSpawned":
                            // Show message that player Spawned
                            gameBoard.PlayerJoinedMessage(parsedMessage.Content);
                            break;
                        default:
                            break;
                    }
                }

            }
            catch(System.Exception e)
            {
                Debug.Log($"Error parsing message: {jsonContent}. Exception message: {e.Message}");
            }
        }
    }
}