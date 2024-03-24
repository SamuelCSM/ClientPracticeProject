using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Text;

using ALPackage;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace ALPackage
{
    //游戏总管理类
    public class ALHttpSingleDownloadDealer
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

        public ALHttpSingleDownloadDealer(string _url, string _outputPath, Action _doneDelegate, Action<int> _failDelegate, int _retryCount = 3, int _timeoutMs = 8000, int _readWriteTimeoutMs = 8000)
        {
            _m_bIsStarted = false;

            _m_iOPSerialzie = SerializeOpMgr.next();

            _m_sURL = _url;
            _m_sOutputPath = _outputPath;
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
            try
            {
                //开启下载线程
                Thread downloadThread = new Thread(_dealDownload);
                downloadThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"开启下载线程失败:{e}");
                _dealFail(-1);
            }
        }

        /// <summary>
        /// 清空资源并放弃下载
        /// </summary>
        public void discard()
        {
            //设置新序列号
            _m_iOPSerialzie = -1;

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

            //开始下载资源 _m_sURL
            bool isSuccess = false;
            Uri u = null;
            HttpWebRequest mRequest = null;
            HttpWebResponse wr = null;
            FileStream fs = null;
            //错误结果状态码
            int errStatus = 0;
            try
            {
                u = new Uri(_m_sURL);
                mRequest = (HttpWebRequest)WebRequest.Create(u);
                mRequest.Timeout = _m_iTimeoutMS;//请求响应时间，
                mRequest.ReadWriteTimeout = _m_iReadWriteTimeoutMS;//read和write函数的响应时间
                mRequest.ServicePoint.Expect100Continue = false;
                mRequest.Method = "GET";
                wr = (HttpWebResponse)mRequest.GetResponse();
                _m_lFileSize = wr.ContentLength;
                Stream sIn = wr.GetResponseStream();
                string outpath = System.IO.Path.GetDirectoryName(_m_sOutputPath);

                //序列号无效直接退出
                if(_m_iOPSerialzie < 0)
                    return;

                try
                {
                    if(!System.IO.Directory.Exists(outpath))
                        System.IO.Directory.CreateDirectory(outpath);
                    try
                    {
                        if(File.Exists(_m_sOutputPath))
                            File.Delete(_m_sOutputPath);
                    }
                    catch (Exception _ex)
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogError($"Delete File Err : {_m_sOutputPath} mes {_ex.Message}");
#endif
                    }
                    fs = new FileStream(_m_sOutputPath, FileMode.Create, FileAccess.Write);

                    //long length = wr.ContentLength;
                    int count = 0;
                    int onceCount = 0;
                    byte[] buffer = null;
                    if(buffer == null)
                    {
                        //注意这里一次保存4KB是有验证过的，如果一次保存1KB保存速度比较慢
                        buffer = new byte[4096];
                    }

                    isSuccess = true;
                    while (true)
                    {
                        // 如果请求出现问题就退出循环
                        int status = (int)wr.StatusCode;
                        if (status < 200 || status >= 300)
                        {
                            isSuccess = false;
                            //设置错误结果状态码
                            errStatus = status;
                            break;
                        }

                        // 如果数据都读取完了就退出循环
                        if ((onceCount = sIn.Read(buffer, 0, buffer.Length)) == 0)
                            break;

                        //判断序列号是否有效，无效表示需要退出
                        if(_m_iOPSerialzie < 0)
                            break;

                        //写入文件
                        fs.Write(buffer, 0, onceCount);
                        count += onceCount;
                        _m_fDownloadBytes = count;
                    }

                    //根据总下载长度判断文件下载是否成功
                    if(count < _m_lFileSize)
                    {
                        //下载一半被discard的话_m_iOPSerialzie是-1，不要报错，因为这是用户希望的操作
                        if (_m_iOPSerialzie >= 0)
                        {
                            Debug.LogError($"下载资源[{_m_sURL}]长度不匹配：{count} - {_m_lFileSize}");
                        }
                        isSuccess = false;
                    }
                }
                finally
                {
                    if(null != sIn)
                        sIn.Close();
                    if(null != fs)
                        fs.Close();
                }
            }
            catch(System.Exception ex)
            {
#if UNITY_EDITOR
                //下载一半被discard的话_m_iOPSerialzie是-1，不要报错，因为这是用户希望的操作
                if (_m_iOPSerialzie >= 0)
                {
                    if (ex is WebException wex)
                    {
                        int statusCode =  wex.Response != null && wex.Response is HttpWebResponse? (int) ((HttpWebResponse) wex.Response).StatusCode : -1;
                        Debug.LogError($"下载资源异常：{wex.Status}:{statusCode}:{_m_sURL} \t {_m_sOutputPath}\n{ex}");
                    }
                    else
                    {
                        Debug.LogError($"下载资源异常：{_m_sURL} \t {_m_sOutputPath}\n{ex}");
                    }
                }
#endif
                isSuccess = false;
            }
            finally
            {
                if(fs != null)
                    fs.Close();
                if(wr != null)
                    wr.Close();
                if(mRequest != null)
                    mRequest.Abort();
            }

            //判断序列号是否有效，无效表示需要删除文件并退出
            if(_m_iOPSerialzie < 0)
            {
                //删除文件
                try
                {
                    File.Delete(_m_sOutputPath);
                }
                catch(Exception) { }

                return;
            }

            if(isSuccess)
            {
                //读取对应的文件
                //处理下载成功操作
                //下一帧处理成功操作
                UniTaskMgr.Instance.createYieldUpdateTask(_dealSuc);
            }
            else
            {
                if(_m_iCanRetryCount > 0)
                {
#if UNITY_EDITOR
                    Debug.LogError($"剩余{_m_iCanRetryCount}次，下载资源失败再次开启下载：{_m_sURL}");
#endif
                    //再次开启加载
                    UniTaskMgr.Instance.createYieldUpdateTask(_retry);

                }
                else
                {
                    //不可重试则输出错误，并调用失败处理
#if UNITY_EDITOR
                    Debug.LogError("下载资源失败超出重试次数：" + _m_sURL);
#endif
                    UniTaskMgr.Instance.createYieldUpdateTask(() => { _dealFail(errStatus); });
                }
            }
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

