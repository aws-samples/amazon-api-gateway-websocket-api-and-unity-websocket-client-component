/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;


namespace Amazon.Unity
{
    public class AmazonAPIGatewayWebSocketManager : MonoBehaviour
    {
        #region NESTED CLASSES

        /// <summary>
        /// Class used for structuring actions to send to the API Gateway WebSocket API
        /// </summary>
        public class WebSocketAction
        {
            /// <summary>
            /// Amazon API Gateway action string used to determine route
            /// </summary>
            public string action;

            /// <summary>
            /// JSON stringified Message
            /// </summary>
            public string message;

            public WebSocketAction(string action, string message)
            {
                this.action = action;
                this.message = message;
            }

        }

        public class ErrorEventArgs : EventArgs
        {
            public string Message { get => _message; }
            public Exception Exception { get => _exception; }

            private string _message;
            private Exception _exception;

            internal ErrorEventArgs(string message)
            {
                _message = message;
            }

            internal ErrorEventArgs(Exception exception, string message)
            {
                _exception = exception;
                _message = message;
            }
        }

        public class MessageEventArgs : EventArgs
        {
            public string Content { get => _content; }

            private string _content;

            internal MessageEventArgs(string content)
            {
                _content = content;
            }
        }

        #endregion

        #region PUBLIC EVENTS

        public event EventHandler OnClose;

        public event EventHandler<ErrorEventArgs> OnError;

        public event EventHandler<MessageEventArgs> OnMessage;

        public event EventHandler OnOpen;

        #endregion

        #region PROPERTIES

        [Tooltip("Keep alive heartbeat period (seconds). 0=disabled")]
        public float HeartbeatPeriod = 0f;

        // WebSocket manager state

        /// <summary>
        /// Instance reference for singleton
        /// </summary>
        public static AmazonAPIGatewayWebSocketManager Instance = null;

        /// <summary>
        /// Timestamp of last heartbeat
        /// </summary>
        float lastHeartbeatTimestamp = 0f;

        /// <summary>
        /// Is client connected?
        /// </summary>
        bool isConnected = false;

        /// <summary>
        /// Does the client need to send heartbeat messages?
        /// </summary>
        bool hasHeartbeat = false;

        /// <summary>
        /// Reference to the open WebSocket client
        /// </summary>
        private ClientWebSocket webSocketClient = null;

        /// <summary>
        /// API Gateway Config data
        /// </summary>
        private AWS_APIG_WS_CONFIG APIGatewayConfig;

        /// <summary>
        /// Cognito Config data
        /// </summary>
        private AWS_COGNITO_CONFIG CognitoConfig;

        #endregion

        #region UNITY LIFECYCLE METHODS
        void Awake()
        {
            AWSConfigManager.ConfigurationLoaded += AWSConfigManager_ConfigurationLoaded;
            AmazonCognitoManager.Authenticated += AmazonCognitoManager_Authenticated;
            Instance = this;

            hasHeartbeat = HeartbeatPeriod > 0f;
        }

        private void Update()
        {
            // Send heartbeat if necessary
            if (hasHeartbeat && isConnected && Time.time - lastHeartbeatTimestamp > HeartbeatPeriod)
            {
                SendActionHeartbeat();
            }
        }

        private void OnDestroy()
        {
            if (webSocketClient != null)
                webSocketClient.Dispose();
        }

        private void OnApplicationQuit()
        {
            Instance = null;
        }

        #endregion

        private async Task Connect(string uri)
        {

            try
            {
                webSocketClient = new ClientWebSocket();
                await webSocketClient.ConnectAsync(new Uri(uri), CancellationToken.None);

                Debug.Log("webSocket.State: " + webSocketClient.State);

                isConnected = true;

                // Invoke OnOpen Event
                if (OnOpen != null) { OnOpen(this, EventArgs.Empty); }

                await StartMessageListener();
            }
            catch (Exception exception)
            {
                // Invoke OnError event
                if (OnError != null)
                {
                    OnError(this, new ErrorEventArgs(exception, "WebSocket error."));
                }
                isConnected = false;

                Debug.Log("WebSocket error. Exception: " + exception.Message);
            }

        }

        private async Task StartMessageListener()
        {

            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);

            while (webSocketClient.State == WebSocketState.Open)
            {

                // Check for messages
                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result = null;
                    do
                    {
                        result = await webSocketClient.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {

                            string action = reader.ReadToEnd();

                            // Invoke OnMessage Event
                            if (OnMessage != null) OnMessage(this, new MessageEventArgs(action));

                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        isConnected = false;
                        // Invoke OnClose Event
                        if (OnClose != null) OnClose(this, EventArgs.Empty);
                    }
                }
            }
        }


        private async Task Send(string message)
        {
            var encoded = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

            await webSocketClient.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void AWSConfigManager_ConfigurationLoaded(AWSConfigClient client)
        {
            if (client.API_Gateway != null) APIGatewayConfig = client.API_Gateway;
            if (client.Cognito != null) CognitoConfig = client.Cognito;

        }

        private void AmazonCognitoManager_Authenticated()
        {
            string url = $"{APIGatewayConfig.WebSocket_API_URL}?Authorization={CognitoConfig.AccessToken}&clientId={CognitoConfig.AppClientID}&userPoolId={CognitoConfig.CognitoUserPool}";
            _ = Connect(url);
        }


        private void SendActionHeartbeat()
        {
            // create the object
            var actionObject = new WebSocketAction(action: "heartbeat", message: "");
            var jsonString = JsonUtility.ToJson(actionObject);

            _ = Send(jsonString);

            lastHeartbeatTimestamp = Time.time;
        }

        public void SendActionMessage(string jsonContent)
        {
            // create the object
            var actionObject = new WebSocketAction(action: "message", message: jsonContent);
            var jsonString = JsonUtility.ToJson(actionObject);

            _ = Send(jsonString);
        }

    }

}
