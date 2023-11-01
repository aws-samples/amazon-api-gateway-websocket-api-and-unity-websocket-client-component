/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;

public enum AuthMode { APIKey, Cognito };

namespace Amazon.Unity
{
    [System.Serializable]
    public class AWS_APIG_WS_CONFIG
    {
        public string AWS_Region;
        public string WebSocket_API_URL;
        public AWS_APIG_WS_CONFIG LoadConfig()
        {
            return this;
        }
    }

    [System.Serializable]
    public class AWS_COGNITO_CONFIG
    {
        public string AWS_Region;
        public string CognitoUserPool;
        public string CognitoIdentityPool;
        public string CognitoDomain;
        public string AppClientID;
        static string Token;
        public string IDToken;
        public string RefreshToken;
        public string AccessToken;
        public string RefreshTime;
        public string UserName;
        public static void SaveToken(string token)
        {
            Token = token;
        }
        public static string GetToken()
        {
            return Token;
        }
        public AWS_COGNITO_CONFIG LoadConfig()
        {
            return this;
        }
    }

    [System.Serializable]
    public class AWSConfigClient
    {
        public static AWSConfigClient Instance;
        public AWS_COGNITO_CONFIG Cognito;
        public AWS_APIG_WS_CONFIG API_Gateway;
        public string configPath;
        public static AWSConfigClient LoadConfig(string configPath)
        {
            Instance = new AWSConfigClient();
            if (File.Exists(configPath))
            {
                try
                {
                    Debug.Log("Amplify Configuration Loading from file");
                    StreamReader reader = new StreamReader(configPath);
                    string readJson = reader.ReadToEnd();
                    Instance = JsonConvert.DeserializeObject<AWSConfigClient>(readJson);
                }
                catch (Exception error)
                {
                    Debug.Log(error.Message);
                    Debug.Log("Loading JSON Failed");
                }
            }
            else
            {
                Debug.LogWarning("No Config File at " + configPath);
                Instance = null;
                return null;
            }
            return Instance;
        }
        public void SaveConfig()
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(this));
        }
    }
    [System.Serializable]
    public class AWSConfigManager : MonoBehaviour
    {
        [Tooltip("Loads from this inspector manager versus a configuration file (good for starting new)")]
        public bool loadFromManager = false;
        [Tooltip("Configure here (make sure load from manager is true) or view your current file configuration")]
        public AWSConfigClient AWSConfiguration;

        //This is a delegate and event for whenever the configuration is loaded. 
        public delegate void ConfigurationLoadedDel(AWSConfigClient client);
        public static event ConfigurationLoadedDel ConfigurationLoaded = delegate { };

        //Persistent datapath to be available. 
        public string configPath;

        public static AWSConfigManager Instance = null;
        public void Start()
        {

            //Get persistent datapath for device;
            configPath = Application.persistentDataPath + "/aws-config.json";

            //Load configuration based upon path OR from the Inspector Manager Script;
            if (!loadFromManager)
            {
                AWSConfiguration = AWSConfigClient.LoadConfig(configPath);
                if (AWSConfiguration != null)
                {
                    ConfigurationLoaded?.Invoke(AWSConfiguration);
                }
            }
            else
            {
                AWSConfigClient.Instance = AWSConfiguration;
                ConfigurationLoaded?.Invoke(AWSConfiguration);
                Debug.Log("Configuration Loaded");
            }
            //This static Instance allows for quick access across the application of the configuration;
            Instance = this;
        }

        public void OnApplicationQuit()
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(AWSConfiguration));
        }
    }
}