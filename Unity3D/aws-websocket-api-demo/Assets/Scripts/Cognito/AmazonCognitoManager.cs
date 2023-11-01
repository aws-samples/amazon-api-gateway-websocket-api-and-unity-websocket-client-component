/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System;
using System.Net;
using TMPro;

namespace Amazon.Unity
{
    public class AmazonCognitoManager : MonoBehaviour
    {
        public static AmazonCognitoManager Instance;
        public static AWS_COGNITO_CONFIG COGNITO_CONFIG;
        AWS_COGNITO_CONFIG baseConfig;
        public delegate void AuthenticatedDel();
        public static event AuthenticatedDel Authenticated;
        public static string SessionToken;
        static string cognitoidpurl = "https://cognito-idp.";
        static string cognitoidp_uri = "cognito-idp.";
        static string cognitoidentityurl = "https://cognito-identity.";
        static string amazonendurl = ".amazonaws.com/";
        string region = "us-east-1";
        public bool editorTesting = false;
        public string _username;
        public string _password;
        private bool isLoaded = false, isLoginReady = false;
        public GameObject LoginPanel, LoginButton;
        public TMP_InputField UsernameInput, PasswordInput;


        void Awake()
        {
            AWSConfigManager.ConfigurationLoaded += AWSConfigManager_ConfigurationLoaded;
            Instance = this;
            Authenticated += AuthenticatedLauncher;
        }

        void AuthenticatedLauncher()
        {
            LoginPanel?.SetActive(String.IsNullOrEmpty(SessionToken));
        }

        public async void AWSConfigManager_ConfigurationLoaded(AWSConfigClient client)
        {
            isLoaded = true;
            Debug.Log("Cognito Manager Received Config");
            COGNITO_CONFIG = client.Cognito;
            baseConfig = COGNITO_CONFIG;
            region = baseConfig.AWS_Region;
            if (!editorTesting)
            {
                if (await CheckLogin(client.Cognito))
                {
                    Debug.Log("Refreshed Identity");
                    return;
                }
                else if (editorTesting)
                {
                    try
                    {
                        Login(_username, _password);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);
                    }
                }
            }
            else
            {
                try
                {
                    Login(_username, _password);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
            }
        }

        public void Logout()
        {
            SessionToken = null;
            COGNITO_CONFIG.IDToken = null;
            COGNITO_CONFIG.AccessToken = null;
            COGNITO_CONFIG.RefreshToken = null;
            _password = null;
            LoginPanel?.SetActive(String.IsNullOrEmpty(SessionToken));
        }

        public void SetUserName(string _value)
        {
            _username = _value;
            if (LoginButton != null)
            {
                LoginButton.SetActive((_username.Length > 5 && _password.Length > 5));
            }
        }

        public void SetPassword(string _value)
        {
            _password = _value;
            LoginButton?.SetActive((_username.Length > 5 && _password.Length > 5));
        }

        public static async Task<string> Refresh_Token(AWSConfigClient configClient)
        {
            var baseConfig = configClient.Cognito;
            COGNITO_CONFIG = configClient.Cognito;
            if (COGNITO_CONFIG.RefreshToken == null)
            {
                Debug.Log("No Refresh Token");
                return null;
            }
            var region = baseConfig.AWS_Region;
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            JObject authParameters = new JObject();
            JObject _params = new JObject();
            _params.Add("AuthFlow", "REFRESH_TOKEN_AUTH");
            authParameters.Add("REFRESH_TOKEN", COGNITO_CONFIG.RefreshToken);
            _params.Add("ClientId", baseConfig.AppClientID);
            _params.Add("AuthParameters", authParameters);
            string url1 = cognitoidpurl + region + amazonendurl;
            string target = "AWSCognitoIdentityProviderService.InitiateAuth";
            string idtoken = "";
            try
            {
                var InitAuth = await CogRequest(client, _params, target, url1);
                idtoken = InitAuth["AuthenticationResult"]["IdToken"].ToString();
                COGNITO_CONFIG.IDToken = idtoken;
                COGNITO_CONFIG.AccessToken = InitAuth["AuthenticationResult"]["AccessToken"].ToString();
                COGNITO_CONFIG.RefreshTime = DateTime.UtcNow
                    .AddSeconds(Int32.Parse(InitAuth["AuthenticationResult"]["ExpiresIn"].ToString())).Ticks.ToString();
            }
            catch
            {
                Debug.Log("No Auth");
                throw new Exception("Refresh Failed");
            }
            client.Dispose();
            await GetUserAsync(COGNITO_CONFIG.AccessToken);
            SessionToken = idtoken;
            Debug.Log("Authenticated");
            return idtoken;
        }
        public static async Task<AWSConfigClient> Login(string username, string password, AWSConfigClient configClient)
        {
            if (configClient == null)
            {
                Debug.Log("Config Client Not Loaded Properly");
                return null;
            }
            var baseConfig = configClient.Cognito;
            COGNITO_CONFIG = configClient.Cognito;
            var region = baseConfig.AWS_Region;
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            JObject authParameters = new JObject();
            JObject _params = new JObject();
            _params.Add("AuthFlow", "USER_PASSWORD_AUTH");
            authParameters.Add("PASSWORD", password);
            authParameters.Add("USERNAME", username);
            _params.Add("ClientId", baseConfig.AppClientID);
            _params.Add("AuthParameters", authParameters);
            string url1 = cognitoidpurl + region + amazonendurl;
            string target = "AWSCognitoIdentityProviderService.InitiateAuth";
            var InitAuth = await CogRequest(client, _params, target, url1);
            string idtoken = "";
            try
            {
                Debug.Log(InitAuth);
                idtoken = InitAuth["AuthenticationResult"]["IdToken"].ToString();
                COGNITO_CONFIG.IDToken = idtoken;
                COGNITO_CONFIG.RefreshToken = InitAuth["AuthenticationResult"]["RefreshToken"].ToString();
                COGNITO_CONFIG.AccessToken = InitAuth["AuthenticationResult"]["AccessToken"].ToString();
                COGNITO_CONFIG.RefreshTime = DateTime.UtcNow
                    .AddSeconds(Int32.Parse(InitAuth["AuthenticationResult"]["ExpiresIn"].ToString())).Ticks.ToString();
            }
            catch
            {
                Debug.Log("No Auth");
                return null;
            }

            client.Dispose();
            await GetUserAsync(COGNITO_CONFIG.AccessToken);
            SessionToken = idtoken;
            Debug.Log("Authenticated");
            configClient.Cognito = COGNITO_CONFIG;
            return configClient;
        }

        public void Login()
        {
            if (!String.IsNullOrEmpty(_username) && !String.IsNullOrEmpty(_password))
            {
                Login(_username, _password);
            }
        }

        public void UpdateUserName()
        {
            _username = UsernameInput.text;
        }

        public void UpdatePassword()
        {
            _password = PasswordInput.text;
        }

        public async void Login(string username, string password)
        {
            if (isLoaded == false) return;
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            JObject authParameters = new JObject();
            JObject _params = new JObject();
            _params.Add("AuthFlow", "USER_PASSWORD_AUTH");
            authParameters.Add("PASSWORD", password);
            authParameters.Add("USERNAME", username);
            _params.Add("ClientId", baseConfig.AppClientID);
            _params.Add("AuthParameters", authParameters);
            string url1 = cognitoidpurl + region + amazonendurl;
            string target = "AWSCognitoIdentityProviderService.InitiateAuth";
            var InitAuth = await CogRequest(client, _params, target, url1);
            string idtoken = "";
            try
            {
                idtoken = InitAuth["AuthenticationResult"]["IdToken"].ToString();
                COGNITO_CONFIG.IDToken = idtoken;
                COGNITO_CONFIG.RefreshToken = InitAuth["AuthenticationResult"]["RefreshToken"].ToString();
                COGNITO_CONFIG.AccessToken = InitAuth["AuthenticationResult"]["AccessToken"].ToString();
                COGNITO_CONFIG.RefreshTime = DateTime.UtcNow
                    .AddSeconds(Int32.Parse(InitAuth["AuthenticationResult"]["ExpiresIn"].ToString())).Ticks.ToString();
            }


            catch
            {
                Debug.Log("No Auth");
                return;
            }

            client.Dispose();
            await GetUserAsync(COGNITO_CONFIG.AccessToken);
            SessionToken = idtoken;
            Debug.Log("Authenticated");
            Authenticated?.Invoke();
            Instance = this;
        }

        public async Task<bool> CheckLogin(AWS_COGNITO_CONFIG config)
        {
            try
            {
                if (config.RefreshToken.Length > 0)
                {
                    try
                    {
                        await RefreshLogin(config.RefreshToken);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        async static Task<string> GetUserAsync(string accessToken)
        {
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            JObject _params = new JObject();
            _params.Add("AccessToken", accessToken);
            string url1 = cognitoidpurl + COGNITO_CONFIG.AWS_Region + amazonendurl;
            string target = "AWSCognitoIdentityProviderService.GetUser";
            JObject GetUserResponse = new JObject();
            try
            {
                GetUserResponse = await CogRequest(client, _params, target, url1);
                COGNITO_CONFIG.UserName = (string)GetUserResponse["Username"];
            }
            catch
            {
                COGNITO_CONFIG.UserName = null;
                Debug.Log("GetUserAsync Failed");
            }

            client.Dispose();
            return GetUserResponse.ToString();
        }

        async Task<string> RefreshLogin(string refreshToken)
        {
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            JObject authParameters = new JObject();
            JObject _params = new JObject();
            _params.Add("AuthFlow", "REFRESH_TOKEN_AUTH");
            authParameters.Add("REFRESH_TOKEN", refreshToken);
            _params.Add("ClientId", baseConfig.AppClientID);
            _params.Add("AuthParameters", authParameters);
            string url1 = cognitoidpurl + region + amazonendurl;
            string target = "AWSCognitoIdentityProviderService.InitiateAuth";
            string idtoken = "";
            try
            {
                var InitAuth = await CogRequest(client, _params, target, url1);
                idtoken = InitAuth["AuthenticationResult"]["IdToken"].ToString();
                COGNITO_CONFIG.IDToken = idtoken;
                COGNITO_CONFIG.AccessToken = InitAuth["AuthenticationResult"]["AccessToken"].ToString();
                COGNITO_CONFIG.RefreshTime = DateTime.UtcNow
                    .AddSeconds(Int32.Parse(InitAuth["AuthenticationResult"]["ExpiresIn"].ToString())).Ticks.ToString();
            }
            catch
            {
                Debug.Log("No Auth");
                throw new Exception("Refresh Failed");
            }
            client.Dispose();
            await GetUserAsync(COGNITO_CONFIG.AccessToken);
            SessionToken = idtoken;
            Debug.Log("Authenticated");
            if (!editorTesting) Authenticated?.Invoke();
            return idtoken;
        }

        private static async Task<JObject> CogRequest(HttpClient client, JObject _params, string _target, string url)
        {
            client.DefaultRequestHeaders.Add("X-Amz-Target", _target);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-amz-json-1.1"));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(_params.ToString(), Encoding.UTF8, "application/x-amz-json-1.1");
            var aclient = await client.SendAsync(request);
            //ExtensionMethods.ParseResponse(aclient);
            JObject response = await readResult(aclient.Content);
            return response;
        }

        static async Task<JObject> readResult(HttpContent content)
        {
            string result = await content.ReadAsStringAsync();
            return JObject.Parse(result);
        }

    }
}