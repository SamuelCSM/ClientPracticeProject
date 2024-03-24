using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

public class Launch : MonoBehaviour
{
    /// <summary>
    /// 资源系统运行模式
    /// </summary>
    public EPlayMode playMode = EPlayMode.EditorSimulateMode;

    void Awake()
    {
        Debug.Log($"资源系统运行模式：{playMode}");
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    private async UniTaskVoid Start()
    {
        await InitPackage();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private async UniTask InitPackage()
    {
        // 初始化资源系统
        YooAssets.Initialize();

        string packageName = "DefaultPackage";
        // 创建默认的资源包（原生文件）
        var package = YooAssets.CreatePackage(packageName);
        // 创建默认的资源包（资源文件）
        var resourcesPackage = YooAssets.CreatePackage("ResourcesPackage");

        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(package);

        // 编辑器下的模拟模式
        InitializationOperation initializationOperation = null;
        InitializationOperation initializationResOperation = null;
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var createParameters = new EditorSimulateModeParameters();
            createParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, packageName);
            initializationOperation = package.InitializeAsync(createParameters);
            initializationResOperation = resourcesPackage.InitializeAsync(createParameters);
        }

        // 单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var createParameters = new OfflinePlayModeParameters();
            createParameters.DecryptionServices = new FileStreamDecryption();
            initializationOperation = package.InitializeAsync(createParameters);
            initializationResOperation = resourcesPackage.InitializeAsync(createParameters);
        }

        // 联机运行模式
        if (playMode == EPlayMode.HostPlayMode)
        {
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            var createParameters = new HostPlayModeParameters();
            createParameters.DecryptionServices = new FileStreamDecryption();
            createParameters.BuildinQueryServices = new GameQueryServices();
            createParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            initializationOperation = package.InitializeAsync(createParameters);
            initializationResOperation = resourcesPackage.InitializeAsync(createParameters);
        }

        // WebGL运行模式
        if (playMode == EPlayMode.WebPlayMode)
        {
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            var createParameters = new WebPlayModeParameters();
            createParameters.DecryptionServices = new FileStreamDecryption();
            createParameters.BuildinQueryServices = new GameQueryServices();
            createParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            initializationOperation = package.InitializeAsync(createParameters);
            initializationResOperation = resourcesPackage.InitializeAsync(createParameters);
        }

        await UniTask.WaitUntil(() => initializationOperation.IsDone && initializationResOperation.IsDone);

        // 如果初始化失败弹出提示界面
        if (initializationOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogWarning($"{initializationOperation.Error}"); 
            return;
        }

        // 资源版本信息
        var version = initializationOperation.PackageVersion;
        Debug.Log($"Init resource package version : {version}");

        // 获取资源版本信息
        var operation = package.UpdatePackageVersionAsync(false);
        await UniTask.WaitUntil(() => operation.IsDone);

        if (operation.Status != EOperationStatus.Succeed)
        {
            //更新失败
            Debug.LogError(operation.Error);
            return;
        }

        //更新成功
        string packageVersion = operation.PackageVersion;
        Debug.Log($"Updated package Version : {packageVersion}");

        bool savePackageVersion = true;
        // 更新资源清单
        var updatePackageManifestoperation = package.UpdatePackageManifestAsync(packageVersion, savePackageVersion);
        await UniTask.WaitUntil(() => updatePackageManifestoperation.IsDone);

        if (updatePackageManifestoperation.Status == EOperationStatus.Succeed)
        {
            //更新成功
        }
        else
        {
            //更新失败
            Debug.LogError(updatePackageManifestoperation.Error);
        }

        // 开始下载补丁包
        await Download();

        // 切换到主页面场景
        //string location = "scene_home";
        //var sceneMode = UnityEngine.SceneManagement.LoadSceneMode.Single;
        //bool suspendLoad = false;
        //SceneHandle handle = package.LoadSceneAsync(location, sceneMode, suspendLoad);
        //await UniTask.WaitUntil(() => handle.IsDone);
        //Debug.Log($"Scene name is {handle.SceneObject.name}");

        HybirdCLRMgr.Instance.Start();
    }

    /// <summary>
    /// 获取资源服务器地址
    /// </summary>
    private string GetHostServerURL()
    {
        //string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
        string hostServerIP = "http://127.0.0.1";
        string appVersion = "v1.0";

#if UNITY_EDITOR
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            return $"{hostServerIP}/CDN/Android/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            return $"{hostServerIP}/CDN/IPhone/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
            return $"{hostServerIP}/CDN/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/CDN/PC/{appVersion}";
#else
        if (Application.platform == RuntimePlatform.Android)
            return $"{hostServerIP}/CDN/Android/{appVersion}";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return $"{hostServerIP}/CDN/IPhone/{appVersion}";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return $"{hostServerIP}/CDN/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/CDN/PC/{appVersion}";
#endif
    }

    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }

    /// <summary>
    /// 资源文件流加载解密类
    /// </summary>
    private class FileStreamDecryption : IDecryptionServices
    {
        /// <summary>
        /// 同步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
        {
            BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            managedStream = bundleStream;
            return AssetBundle.LoadFromStream(bundleStream, fileInfo.ConentCRC, GetManagedReadBufferSize());
        }

        /// <summary>
        /// 异步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
        {
            BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            managedStream = bundleStream;
            return AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.ConentCRC, GetManagedReadBufferSize());
        }

        private static uint GetManagedReadBufferSize()
        {
            return 1024;
        }
    }

    /// <summary>
    /// 资源文件偏移加载解密类
    /// </summary>
    private class FileOffsetDecryption : IDecryptionServices
    {
        /// <summary>
        /// 同步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
        {
            managedStream = null;
            return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
        }

        /// <summary>
        /// 异步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
        {
            managedStream = null;
            return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
        }

        private static ulong GetFileOffset()
        {
            return 32;
        }
    }

    private async UniTask Download()
    {
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        var package = YooAssets.GetPackage("DefaultPackage");
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

        //没有需要下载的资源
        if (downloader.TotalDownloadCount == 0)
        {
            return;
        }

        //需要下载的文件总数和总大小
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;

        //注册回调方法
        downloader.OnDownloadErrorCallback = OnDownloadErrorFunction;
        downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction;
        downloader.OnDownloadOverCallback = OnDownloadOverFunction;
        downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction;

        //开启下载
        downloader.BeginDownload();
        await UniTask.WaitUntil(() => downloader.IsDone);

        //检测下载结果
        if (downloader.Status == EOperationStatus.Succeed)
        {
            //下载成功
            Debug.Log("下载更新成功");
        }
        else
        {
            //下载失败
            Debug.Log("下载更新失败");
        }
    }

    // 下载错误
    private void OnDownloadErrorFunction(string filename, string error)
    {
        Debug.Log($"下载错误，文件名：{filename}, 错误信息：{error}");
    }

    // 下载进度更新
    private void OnDownloadProgressUpdateFunction(int totaldownloadcount, int currentdownloadcount, long totaldownloadbytes, long currentdownloadbytes)
    {
        Debug.Log($"文件总数：{totaldownloadcount}, 已下载文件数量：{currentdownloadcount}, 下载总大小：{totaldownloadbytes}, 已下载大小：{currentdownloadbytes}");
    }

    // 下载结束
    private void OnDownloadOverFunction(bool issucceed)
    { 
        Debug.Log($"下载{(issucceed ? "成功" : "失败")}");
    }

    // 开始下载文件
    private void OnStartDownloadFileFunction(string filename, long sizebytes)
    {
        Debug.Log($"开始下载文件: 文件名：{filename}, 文件大小：{sizebytes}");
    }
}
