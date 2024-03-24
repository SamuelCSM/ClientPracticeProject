using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

public class HybirdCLRMgr
{
    // 单例
    private static HybirdCLRMgr _instance;

    public static HybirdCLRMgr Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new HybirdCLRMgr();
            }
            return _instance;
        }
    }

    public async UniTask Start()
    {
        await LoadAssetsAsync(() => { Debug.Log("hotupdate dll suc done.");});
        await StartGame();
    }

    #region download assets

    private static Dictionary<string, byte[]> s_assetDatas = new Dictionary<string, byte[]>();

    public static byte[] ReadBytesFromStreamingAssets(string dllName)
    {
        return s_assetDatas[dllName];
    }

    private string GetWebRequestPath(string asset)
    {
        var path = $"{Application.streamingAssetsPath}/{asset}";
        if (!path.Contains("://"))
        {
            path = "file://" + path;
        }
        return path;
    }
    private static List<string> AOTMetaAssemblyFiles { get; } = new List<string>()
    {
        "mscorlib.dll",
        "System.dll",
        "System.Core.dll",
    };

    public async UniTask LoadAssetsAsync(Action onDownloadComplete)
    {
        var assets = new List<string>
        {
            "HotUpdate.dll",
        }.Concat(AOTMetaAssemblyFiles);

        var package = YooAssets.GetPackage("DefaultPackage");

        foreach (var asset in assets)
        {
            // Or retrieve results as binary data
            RawFileHandle handle = package.LoadRawFileAsync(asset);
            await UniTask.WaitUntil(() => handle.IsDone);
            byte[] assetData = handle.GetRawFileData();
            Debug.Log($"dll:{asset}  size:{assetData.Length}");
            s_assetDatas[asset] = assetData;
        }

        if (onDownloadComplete != null) 
            onDownloadComplete();
    }

    #endregion

    private static Assembly _hotUpdateAss;

    /// <summary>
    /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
    /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
    /// </summary>
    private static void LoadMetadataForAOTAssemblies()
    {
        /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
        /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
        /// 
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in AOTMetaAssemblyFiles)
        {
            byte[] dllBytes = ReadBytesFromStreamingAssets(aotDllName);
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        }
    }

    async UniTask StartGame()
    {
        LoadMetadataForAOTAssemblies();

#if !UNITY_EDITOR
        _hotUpdateAss = Assembly.Load(ReadBytesFromStreamingAssets("HotUpdate.dll"));
#else
        _hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif
        Type entryType = _hotUpdateAss.GetType("Entry");
        entryType.GetMethod("Start").Invoke(null, null);

        Run_InstantiateComponentByAsset();

        await DelayAndQuit();
    }

    async UniTask DelayAndQuit()
    {
#if UNITY_STANDALONE_WIN
        File.WriteAllText(Directory.GetCurrentDirectory() + "/run.log", "ok", System.Text.Encoding.UTF8);
#endif
        for (int i = 10; i >= 1; i--)
        {
            UnityEngine.Debug.Log($"将于{i}s后自动退出");
            await UniTask.Delay(TimeSpan.FromSeconds(1));
        }
        Application.Quit();
    }

    private void Run_InstantiateComponentByAsset()
    {
        // 通过实例化assetbundle中的资源，还原资源上的热更新脚本
        //AssetBundle ab = AssetBundle.LoadFromMemory(ReadBytesFromStreamingAssets("prefabs"));
        //GameObject cube = ab.LoadAsset<GameObject>("HybridCLRPrefab");
        //GameObject.Instantiate(cube);
        Debug.Log($"Run_InstantiateComponentByAsset");

        var package = YooAssets.GetPackage("ResourcesPackage");
        AssetHandle handle = package.LoadAssetAsync<GameObject>("HybridCLRPrefab");
        handle.Completed += (_handle) =>
        {
            GameObject go = _handle.InstantiateSync();
            Debug.Log($"Prefab name is {go.name}");
        };
    }
}

