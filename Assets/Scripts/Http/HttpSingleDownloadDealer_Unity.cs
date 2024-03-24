using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

using ALPackage;
using System.Threading;
using UnityEngine.Networking;


namespace MG
{
    //游戏总管理类
    public class ALHttpSingleDownloadDealer_Unity
    {
        private int _m_iOPSerialzie;

        private string _m_sURL;
        private string _m_sOutputPath;
        private Action _m_dDoneDelegate;
        private Action<int> _m_dFailDelegate;

        private bool _m_bIsStarted;

        //下载进度
        private long _m_fDownloadBytes;
        private long _m_lFileSize;

        //可以重试的次数
        private int _m_iCanRetryCount;
        //请求超时时间（毫秒）
        private int _m_iTimeoutMS;
        //读写超时时间（毫秒）
        private int _m_iReadWriteTimeoutMS;

        private UnityWebRequest _m_uwr;
        private static HashSet<string> _g_outputPathHistory = new HashSet<string>(); 

        public ALHttpSingleDownloadDealer_Unity(string _url, string _outputPath, Action _doneDelegate, Action<int> _failDelegate, int _retryCount = 3, int _timeoutMs = 8000, int _readWriteTimeoutMs = 8000)
        {
            _m_bIsStarted = false;

            _m_iOPSerialzie = SerializeOpMgr.next();

            _m_sURL = _url;
            _m_sOutputPath = _outputPath;
            if (_g_outputPathHistory.Contains(_outputPath))
            {
                Debug.LogError($"_outputPath重复了：{_outputPath}");
            }
            else
            {
                _g_outputPathHistory.Add(_outputPath);
            }
            _m_dDoneDelegate = _doneDelegate;
            _m_dFailDelegate = _failDelegate;

            _m_fDownloadBytes = 0;
            _m_lFileSize = 0;

            _m_iCanRetryCount = _retryCount;
            if(_m_iCanRetryCount > 10)
                _m_iCanRetryCount = 10;
            
            _m_iTimeoutMS = _timeoutMs;
            _m_iReadWriteTimeoutMS = _readWriteTimeoutMs;
        }

        public long fileSize { get { return _m_lFileSize; } }
        public long downloadedBytes { get { return (long)(_m_fDownloadBytes); } }

        /// <summary>
        /// 获取下载进度情况
        /// </summary>
        public float process
        {
            get
            {
                if(0 == _m_lFileSize)
                    return 0f;

                //计算倍率，先放大再除
                return ((_m_fDownloadBytes * 10000) / _m_lFileSize) / 10000f;
            }
        }

        /****************
         * 开启任务执行
         **/
        public void startLoad()
        {
            if(_m_bIsStarted)
                return;

            _m_bIsStarted = true;

            //开启下载线程
            _startDealDownlLoad();
        }

        /****************
         * 内部执行任务的函数
         **/
        protected void _startDealDownlLoad()
        {
            _dealDownload();
        }

        /// <summary>
        /// 清空资源并放弃下载
        /// </summary>
        public void discard()
        {
            //设置新序列号
            _m_iOPSerialzie = -1;

            if (_m_uwr != null)
            {
                _m_uwr.Dispose();
                _m_uwr = null;
            }
            
            _m_iCanRetryCount = 0;

            //处理失败回调
            _dealFail(0);
        }

        /// <summary>
        /// 初始化DLL操作
        /// </summary>
        /// <param name="_downloadURL">下载的DLL连接地址</param>
        protected void _dealDownload()
        {
            //序列号无效直接退出
            if(_m_iOPSerialzie < 0)
                return;
            if (_m_uwr != null)
            {
                _m_uwr.Dispose();
                _m_uwr = null;
            }
            
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(_m_sOutputPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_m_sOutputPath));
            }

            _m_uwr = new UnityWebRequest(_m_sURL);
            _m_uwr.method = UnityWebRequest.kHttpVerbGET;
            DownloadHandlerFile dh = new DownloadHandlerFile(_m_sOutputPath);//_m_uwr.disposeDownloadHandlerOnDispose默认是true，所以不需要手动调用dispose
            dh.removeFileOnAbort = true;
            _m_uwr.downloadHandler = dh;
            
            var asyncOp = _m_uwr.SendWebRequest();
            asyncOp.completed += operation =>
            {
                if (Thread.CurrentThread.ManagedThreadId != 1)
                {
                    Debug.LogError($"下载完成不在主线程{Thread.CurrentThread.ManagedThreadId}");
                }
                try
                {
                    //判断序列号是否有效，无效表示需要删除文件并退出
                    if (_m_iOPSerialzie < 0)
                    {
                        //删除文件
                        File.Delete(_m_sOutputPath);
                        _dealFail(-2);
                        return;
                    }

#if UNITY_2020
                    if (_m_uwr.result == UnityWebRequest.Result.ConnectionError
                        || _m_uwr.result == UnityWebRequest.Result.DataProcessingError
                        || _m_uwr.result == UnityWebRequest.Result.ProtocolError)
#else
                    if (_m_uwr.isNetworkError || _m_uwr.isHttpError)
#endif
                    {
                        Debug.LogError("download err:" + _m_uwr.error);

                        if (_m_iCanRetryCount > 0)
                        {
#if UNITY_EDITOR
                            Debug.LogError($"剩余{_m_iCanRetryCount}次，下载资源失败再次开启下载：{_m_sURL}");
#endif
                            //再次开启加载
                            _retry();
                        }
                        else
                        {
                            //不可重试则输出错误，并调用失败处理
#if UNITY_EDITOR
                            Debug.LogError("下载资源失败超出重试次数：" + _m_sURL);
#endif
                            _dealFail((int) _m_uwr.responseCode);
                        }
                    }
                    else
                    {
                        long length = new FileInfo(_m_sOutputPath).Length;
                        Debug.Log($"Download saved to: {_m_sOutputPath}:{length}\r\n{_m_uwr.error}");
                        _dealSuc();
                    }

                }
                catch (Exception e)
                {
                    Debug.LogError($"下载错误：{e}");
                    _dealFail(-3);
                }
                finally
                {
                    //上面_dealSuc之后，外面的Dispose会被调用，这里就会被置空
                    if (_m_uwr != null) 
                        _m_uwr.Dispose();
                }
            };
        }

        /***************
         * 进行重试处理
         **/
        protected void _retry()
        {
            _m_iCanRetryCount--;

            //开始下载
            _startDealDownlLoad();
        }

        /// <summary>
        /// 结果处理
        /// </summary>
        protected void _dealSuc()
        {
            Debug.Log($"【{Time.frameCount}】[HTTP] download done:{_m_sURL}");
            

            if(null != _m_dDoneDelegate)
                _m_dDoneDelegate();

            _m_dDoneDelegate = null;
            _m_dFailDelegate = null;
        }
        /// <summary>
        /// 结果处理
        /// </summary>
        protected void _dealFail(int _errStat)
        {
            // 删除
            try
            {
                if (!string.IsNullOrEmpty(_m_sOutputPath))
                {  
                    Debug.Log($"[Http] 删除下载失败的文件：{_m_sOutputPath}");

                    File.Delete(_m_sOutputPath);
                }
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"_dealFail failed, {_m_sOutputPath}: {e}");
#endif
            }

            if (null != _m_dFailDelegate)
                _m_dFailDelegate(_errStat);

            _m_dDoneDelegate = null;
            _m_dFailDelegate = null;
        }
    }
}

