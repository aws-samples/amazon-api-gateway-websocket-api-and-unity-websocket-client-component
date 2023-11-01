/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using System.Net;

public static class ExtensionMethods
{
    public static void SetActiveExt(this GameObject obj, bool isActive)
    {
        if (obj != null)
        {
            obj.SetActive(isActive);
        }
    }
    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> obj, int N)
    {
        return obj.Skip(Math.Max(0, obj.Count() - N));
    }
    public static string TakeLastString<T>(this IEnumerable<T> obj, int N, string J)
    {
        var arr = obj.Skip(Math.Max(0, obj.Count() - N));
        return String.Join(J, arr);
    }

    public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<object>();
        asyncOp.completed += obj => { tcs.SetResult(null); };
        return ((Task)tcs.Task).GetAwaiter();
    }

    public static string EncodeBase64(this string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(valueBytes);
    }

    public static string DecodeBase64(this string value)
    {
        var valueBytes = System.Convert.FromBase64String(value);
        return Encoding.UTF8.GetString(valueBytes);
    }

    public static string SerializeObject(this object obj, bool debug = false)
    {
        string response = JsonConvert.SerializeObject(obj);
        if (debug) Debug.Log(response);
        return response;
    }
    public static T ConvertObject<T>(this object obj, bool debug = false)
    {
        string value = JsonConvert.SerializeObject(obj);
        if (debug) Debug.Log(value);
        try
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
        catch (Exception e)
        {
            Debug.LogError("Deserialization failed \n" + e.Message + "\n" + value);
            string json = null;
            return (T)Convert.ChangeType(json, typeof(T));
        }
    }
    public static T DeserializeString<T>(this string value)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
        catch (Exception e)
        {
            Debug.LogError("Deserialization failed \n" + e.Message + "\n" + value);
            string json = null;
            return (T)Convert.ChangeType(json, typeof(T));
        }
    }
    public static string removeAllNonAlphanumericCharsExceptDashes(this string value)
    {
        // doesn't remove dashes "-"
        Regex rgx = new Regex("[^a-zA-Z0-9 -]");
        value = rgx.Replace(value, "");
        return value;
    }
    public static string SerializeObject(object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }
    public static JObject GetJObj(string json)
    {
        return JObject.Parse(json);
    }
    public static T ToObject<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("ToObject from String Failed \n" + e.Message + "\n" + json);
            string value = null;
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
    public static T ToObject<T>(object obj)
    {
        string json = JsonConvert.SerializeObject(obj);
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("ToObject from Obj Failed \n" + e.Message + "\n" + json);
            string value = null;
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
    public static string ParseResponse(this UnityWebRequest _uwr, bool debug = false, bool throwError = false)
    {
        string response = "";
        switch (_uwr.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                if (debug) Debug.LogWarning("Connection Error");
                response = "Connection Error";
                if (throwError) throw new Exception(response);
                return response;
            case UnityWebRequest.Result.DataProcessingError:
                if (debug) Debug.LogError("Error: " + _uwr.error);
                response = _uwr.error;
                if (throwError) throw new Exception(response);
                return response;
            case UnityWebRequest.Result.ProtocolError:
                if (debug)
                {
                    Debug.LogError("HTTP Error: " + _uwr.error);
                    Debug.LogError(_uwr.downloadHandler.text);
                    Debug.LogError(JObject.FromObject(_uwr.GetResponseHeaders()));
                }
                response = _uwr.error;
                if (throwError) throw new Exception(response);
                return response;
            case UnityWebRequest.Result.Success:
                if (debug) Debug.Log(_uwr.downloadHandler.text);
                response = _uwr.downloadHandler.text;
                return response;
        }
        return response;
    }
    public static void ParseResponse(this UnityWebRequest _uwr, Action<string> action = null, bool debug = false)
    {
        switch (_uwr.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                Debug.LogError("Connection Error");
                break;
            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError("Error: " + _uwr.error);
                break;
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("HTTP Error: " + _uwr.error);
                Debug.LogError(_uwr.downloadHandler.text);
                Debug.LogError(JObject.FromObject(_uwr.GetResponseHeaders()));
                break;
            case UnityWebRequest.Result.Success:
                if (debug) Debug.Log(_uwr.downloadHandler.text);
                if (action != null) action.Invoke(_uwr.downloadHandler.text);
                break;
        }
    }
    public static void ParseResponse(this UnityWebRequest _uwr, UnityEvent<string> action = null, bool debug = false)
    {
        switch (_uwr.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                Debug.LogError("Connection Error");
                break;
            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError("Error: " + _uwr.error);
                break;
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("HTTP Error: " + _uwr.error);
                Debug.LogError(_uwr.downloadHandler.text);
                Debug.LogError(JObject.FromObject(_uwr.GetResponseHeaders()));
                break;
            case UnityWebRequest.Result.Success:
                if (debug) Debug.Log(_uwr.downloadHandler.text);
                if (action != null) action.Invoke(_uwr.downloadHandler.text);
                break;
        }
    }
    public static bool ParseResponse(UnityWebRequest _uwr)
    {
        bool isSuccess = false;
        switch (_uwr.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError("Error: " + _uwr.error);
                break;
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("HTTP Error: " + _uwr.error);
                break;
            case UnityWebRequest.Result.Success:
                Debug.Log("Received: " + _uwr.downloadHandler.text);
                isSuccess = true;
                break;
        }
        return isSuccess;
    }
    public static string ParseResponseToString(UnityWebRequest _uwr)
    {
        string response = null;
        switch (_uwr.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError("Error: " + _uwr.error);
                break;
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("HTTP Error: " + _uwr.error);
                break;
            case UnityWebRequest.Result.Success:
                Debug.Log("Received: " + _uwr.downloadHandler.text);
                response = _uwr.downloadHandler.text;
                break;
        }
        return response;
    }
    public static bool ParseResponse(HttpResponseMessage _uwr)
    {
        bool isSuccess = false;
        switch (_uwr.IsSuccessStatusCode)
        {
            case true:
                isSuccess = true;
                break;
            case false:
                Debug.Log(_uwr.StatusCode + "\n" + _uwr.ReasonPhrase);
                isSuccess = false;
                break;
        }
        return isSuccess;
    }
    public async static Task<(bool, byte[])> ParseResponseByteArr(this HttpResponseMessage _uwr, bool debug = false)
    {
        byte[] response = new byte[0];
        bool isSuccess = false;
        switch (_uwr.IsSuccessStatusCode)
        {
            case true:
                response = await _uwr.Content.ReadAsByteArrayAsync();
                isSuccess = true;
                return (isSuccess, response);
            case false:
                if (debug) Debug.Log(_uwr.StatusCode + "\n" + _uwr.ReasonPhrase);
                isSuccess = false;
                response = null;
                return (isSuccess, response);
        }
    }
    public async static Task<(bool, string)> ParseResponseString(this HttpResponseMessage _uwr, bool debug = false)
    {
        string response = null;
        bool isSuccess = false;
        switch (_uwr.IsSuccessStatusCode)
        {
            case true:
                response = await _uwr.Content.ReadAsStringAsync();
                isSuccess = true;
                return (isSuccess, response);
            case false:
                if (debug) Debug.Log(_uwr.StatusCode + "\n" + _uwr.ReasonPhrase);
                isSuccess = false;
                response = null;
                return (isSuccess, response);
        }
    }

}
[Serializable]
public class EnumerableObject
{
    //Serializable Dictionary
    public string Key, Value;
    public EnumerableObject() { }
    public EnumerableObject(string key, string value) { Key = key; Value = value; }
}
public class EnumerableDictionary
{
    public EnumerableObject[] items;
    public EnumerableDictionary() { }
    public EnumerableDictionary(Dictionary<string, object> dict)
    {
    }
}

[Serializable]
public class SQuaternion
{
    //Serializable Quaternion
    public float x, y, z, w;
    public SQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }
    public Quaternion toQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
    public SQuaternion() { }
}
[Serializable]
public class SVector3
{
    //Serializable Vector3
    public float x, y, z;
    public SVector3(Vector3 vector3)
    {
        x = vector3.x;
        y = vector3.y;
        z = vector3.z;
    }
    public Vector3 toVector3()
    {
        return new Vector3(x, y, z);
    }
    public SVector3(float _x, float _y, float _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }
    public SVector3() { }
}

public class HttpsRequest
{
    public static async Task<byte[]> GetBytes(string url)
    {
        var client = new HttpClient();
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        var clientResponse = await client.SendAsync(request);
        (bool, byte[]) result = await clientResponse.ParseResponseByteArr(true);
        client.Dispose();
        if (result.Item1) return result.Item2;
        else return null;
    }
    public static async Task<string> GetString(string url)
    {
        var client = new HttpClient();
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-amz-json-1.1"));
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        var clientResponse = await client.SendAsync(request);
        var result = await clientResponse.ParseResponseString(true);
        client.Dispose();
        if (result.Item1) return result.Item2;
        else return null;
    }
}
public static class UnityWebRequestAsyncOperationExtension
{

    public static UnityWebRequestAsyncOperationAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOperation)

    {

        return new UnityWebRequestAsyncOperationAwaiter(asyncOperation);

    }
}

public class UnityWebRequestAsyncOperationAwaiter : INotifyCompletion
{

    UnityWebRequestAsyncOperation _asyncOperation;



    public bool IsCompleted

    {

        get { return _asyncOperation.isDone; }

    }



    public UnityWebRequestAsyncOperationAwaiter(UnityWebRequestAsyncOperation asyncOperation)

    {

        _asyncOperation = asyncOperation;

    }



    public void GetResult()

    {

        //NOTE: Since the results can be accessed from UnityWebRequest, is not necessary to return here 

    }



    public void OnCompleted(Action continuation)

    {

        _asyncOperation.completed += _ => { continuation(); };

    }
}
//Code is athttps://github.com/Siccity/SerializableCallback/blob/master/Runtime/InvokableCallback.cs MIT Licensed
[System.Serializable]
public class InvokableUE<TResponse>
{
    public UnityEvent<TResponse> action;
    private InvokableFunction<TResponse> _invokableFunction;
    public string name;
    public TResponse Invoke()
    {
        // _invokableFunction=new InvokableFunction<TResponse>(action);
        return _invokableFunction.Invoke();
    }
}

[System.Serializable]
public class InvokableFunction<TResponse>
{
    public Func<TResponse> func;
    public TResponse Invoke()
    {
        return func();
    }
    public InvokableFunction(object target, string methodName)
    {
        if (target == null || string.IsNullOrEmpty(methodName))
        {
            func = () => default(TResponse);
        }
        else
        {
            func = (Func<TResponse>)Delegate.CreateDelegate(typeof(Func<TResponse>), target, methodName);
        }
    }
}
