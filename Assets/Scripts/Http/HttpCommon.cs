using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;

/// <summary>
/// Http相关操作处理函数接口
/// </summary>
public class ALHttpCommon
{
    /// <summary>
    /// 同步方式获取对应URL返回的内容
    /// </summary>
    /// <param name="_url"></param>
    /// <param name="_parameters"></param>
    /// <param name="_isApplicationJsonMode">是否使用json方式传递参数</param>
    /// <returns></returns>
    public static string GetContentSync(string _url, IDictionary<string, string> _parameters,
        bool _isApplicationJsonMode)
    {
        HttpWebResponse response = _getHttpWebResponse(_url, _parameters, _isApplicationJsonMode);
        if (response == null)
        {
            return null;
        }

        // 如果请求出现问题就退出
        int status = (int)response.StatusCode;
        if (status < 200 || status >= 300)
            return null;

        Stream myResponseStream = null;
        StreamReader myStreamReader = null;
        try
        {
            myResponseStream = response.GetResponseStream();
            myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            return myStreamReader.ReadToEnd();
        }
        catch (Exception e)
        {
            Debug.LogError($"GetContentSync URL[{_url}] Get Exception: {e}");
            return null;
        }
        finally
        {
            response.Dispose();
            myStreamReader?.Close();
            myResponseStream?.Close();
        }
    }

    /// <summary>
    /// 访问对应的URL，将参数使用Post方式传递，并获取返回值
    /// </summary>
    /// <param name="_url"></param>
    /// <param name="_parameters"></param>
    /// <param name="_isApplicationJsonMode"></param>
    /// <returns></returns>
    static HttpWebResponse _getHttpWebResponse(string _url, IDictionary<string, string> _parameters,
        bool _isApplicationJsonMode)
    {
        if (string.IsNullOrEmpty(_url))
            return null;

        string postDataStr = "?";
        if (null != _parameters)
        {
            foreach (string key in _parameters.Keys)
            {
                postDataStr += key + "=" + _parameters[key] + "&";
            }
        }

        postDataStr = postDataStr.Substring(0, postDataStr.Length - 1);
        string url = _url + postDataStr;

        Debug.Log($"_getHttpWebResponse url:{url}");

        Uri myUri = new Uri(url);
        HttpWebRequest request = null;
        try
        {
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(myUri) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(myUri);
            }

            request.Method = "GET";

            if (_isApplicationJsonMode)
            {
                request.ContentType = "application/json";
            }
            else
            {
                request.ContentType = "text/html;charset=UTF-8";
            }

            request.Timeout = 20000;
            return request.GetResponse() as HttpWebResponse;
        }
        catch (Exception e)
        {
            Debug.LogError($"_getHttpWebResponse URL[{_url}] Get Exception: {e}");
            return null;
        }
    }

    /// <summary>
    /// 默认返回值检测函数，默认都返回true
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="certificate"></param>
    /// <param name="chain"></param>
    /// <param name="errors"></param>
    /// <returns></returns>
    private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain,
        SslPolicyErrors errors)
    {
        return true; //总是接受  
    }

    /// <summary>
    /// 异步获取对应URL页面信息的处理函数
    /// </summary>
    /// <param name="_url"></param>
    /// <param name="_onSuc"></param>
    /// <param name="_onFail"></param>
    public static void GetContentAsync(string _url, Action<string> _onSuc, Action<string, int> _onFail)
    {
        GetContentAsync(_url, null, true, _onSuc, _onFail);
    }

    /// <summary>
    /// 异步Get请求，使用HttpWebRequest的BeginGetResponse实现
    /// </summary>
    /// <param name="_url"></param>
    /// <param name="_parameters">请求参数</param>
    /// <param name="_isApplicationJsonMode"></param>
    /// <param name="_onSuc">成功回调，参数是请求结果，目前长度不能大于65535，回调在主线程里执行</param>
    /// <param name="_onFail">失败回调，带有错误原因，回调在主线程里执行</param>
    /// <param name="_logError">是否打印错误</param>
    public static void GetContentAsync(string _url, IDictionary<string, string> _parameters,
        bool _isApplicationJsonMode, Action<string> _onSuc, Action<string, int> _onFail)
    {
        if (string.IsNullOrEmpty(_url))
            return;

        string postDataStr = "?";
        if (null != _parameters)
        {
            foreach (string key in _parameters.Keys)
            {
                postDataStr += key + "=" + _parameters[key] + "&";
            }
        }

        postDataStr = postDataStr.Substring(0, postDataStr.Length - 1);
        string url = _url + postDataStr;

        Debug.Log($"GetAsync url:{url}");

        Uri myUri = null;
        try
        {
            myUri = new Uri(url);
        }
        catch (Exception e)
        {
            Debug.LogError($"GetAsync new Uri URL[{_url}] Get Exception: {e}");

            if (_onFail != null)
            {
                UniTaskMgr.Instance.createYieldUpdateTask(()=> { _onFail($"GetAsync url Error：{url}\n Exception: {e}", 0); }); //转回主线程调用
            }
        }

        HttpWebRequest request = null;
        try
        {
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(myUri) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(myUri);
            }

            request.Method = "GET";

            if (_isApplicationJsonMode)
            {
                request.ContentType = "application/json";
            }
            else
            {
                request.ContentType = "text/html;charset=UTF-8";
            }

            request.Timeout = 20000;

            request.BeginGetResponse(_result =>
            {
                if (_result == null)
                {
                    if (_onFail != null)
                    {
                        UniTaskMgr.Instance.createYieldUpdateTask(()=>
                        {
                            _onFail($"GetAsync url：{url} result null", 0);
                        }); //转回主线程调用
                    }

                    return;
                }

                //直接用闭包的request的话，有时候会有Cannot access a disposed object.的报错，必须要用context传进来
                HttpWebRequest requestContext = (HttpWebRequest)_result.AsyncState;
                HttpWebResponse response = null;
                Stream myResponseStream = null;
                StreamReader myStreamReader = null;
                try
                {
                    response = requestContext.EndGetResponse(_result) as HttpWebResponse;
                    if (response == null)
                    {
                        Debug.LogError($"GetAsync URL[{_url}] response is null");

                        if (_onFail != null)
                        {
                            UniTaskMgr.Instance.createYieldUpdateTask(()=>
                            {
                                _onFail($"GetAsync URL[{_url}] response == null", 0);
                            }); //转回主线程调用
                        }

                        return;
                    }

                    // 如果请求出现问题就退出
                    int status = (int)response.StatusCode;
                    if (status < 200 || status >= 300)
                    {
                        Debug.LogError($"GetAsync URL[{_url}] status == {status}");

                        if (_onFail != null)
                        {
                            UniTaskMgr.Instance.createYieldUpdateTask(() =>
                            {
                                _onFail($"[HTTP]MGHttp GetAsync {_url} status == {status}", status);
                            }); //转回主线程调用
                        }

                        return;
                    }

                    myResponseStream = response.GetResponseStream();
                    myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
                    string retString = myStreamReader.ReadToEnd();

                    if (_onSuc != null)
                    {
                        UniTaskMgr.Instance.createYieldUpdateTask(() => { _onSuc(retString); }); //转回主线程调用
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"GetAsync URL[{_url}] GetResponse Error: {e}");

                    if (_onFail != null)
                    {
                        UniTaskMgr.Instance.createYieldUpdateTask(()=>
                        {
                            _onFail($"GetAsync URL[{_url}] GetResponse Error: {e}", 0);
                        }); //转回主线程调用
                    }
                }
                finally
                {
                    response?.Dispose();
                    myStreamReader?.Close();
                    myResponseStream?.Close();
                }
            }, request);
        }
        catch (Exception e)
        {
            Debug.LogError($"GetAsync URL[{_url}] General Error: {e}");

            if (_onFail != null)
            {
                //转回主线程调用
                UniTaskMgr.Instance.createYieldUpdateTask(()=>
                {
                    _onFail($"GetAsync URL[{_url}] General Error: {e}", 0);
                });
            }
        }
    }


    #region Post

    public static void PostSync(string _url, IDictionary<string, string> _parameters, Action<int, string> _onSucceed,
        Action<string, int> _onFailed)
    {
        HttpWebResponse response = null;
        Stream myResponseStream = null;
        StreamReader myStreamReader = null;

        try
        {
            response = _createPostHttpResponse(_url, _parameters, null, null);
            if (response == null)
            {
                if (_onFailed != null)
                    _onFailed("response is null", 0);
                return;
            }

            // 如果请求出现问题就退出循环
            int status = (int)response.StatusCode;
            if (status < 200 || status >= 300)
            {
                if (_onFailed != null)
                    _onFailed(status.ToString(), status);
                return;
            }

            myResponseStream = response.GetResponseStream();
            myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();

            if (_onSucceed != null)
                _onSucceed(status, retString);
        }
        catch (Exception e)
        {
            if (_onFailed != null)
                _onFailed(e.ToString(), 0);
        }
        finally
        {
            response?.Dispose();
            myStreamReader?.Close();
            myResponseStream?.Close();
        }
    }

    private static HttpWebResponse _createPostHttpResponse(string url, IDictionary<string, string> parameters,
        int? timeout, CookieCollection cookies)
    {
        Debug.Log($"Http Post Request url:[{url}]");

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException("url");
        }

        Uri myUri = new Uri(url);
        HttpWebRequest request = null;
        try
        {
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(myUri) as HttpWebRequest;
                request.ProtocolVersion =
                    HttpVersion.Version10; //来自 https://blog.51cto.com/zhoufoxcn/561934  但是感觉不太合理- -
                // 这里设置了协议类型。https://stackoverflow.com/questions/28286086/default-securityprotocol-in-net-4-5
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 |
                                                        SecurityProtocolType.Tls12;
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(myUri);
            }

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8"; //原来这里没有设置UTF-8感觉有问题

            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            //如果需要POST数据  
            if (!(parameters == null || parameters.Count == 0))
            {
                StringBuilder buffer = new StringBuilder();
                int i = 0;
                foreach (string key in parameters.Keys)
                {
                    if (i > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, parameters[key]);
                    }

                    i++;
                }

                byte[] data = Encoding.UTF8.GetBytes(buffer.ToString());
                request.ContentLength = data.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            return request.GetResponse() as HttpWebResponse;
        }
        catch (Exception e)
        {
            Debug.LogError($"HttpPost 请求{url}失败 e:{e}");
            return null;
        }
    }

    #endregion
}
