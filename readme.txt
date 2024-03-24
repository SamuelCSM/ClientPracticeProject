1、	设置资源。
	打开YooAsset AssetBundleCollector，
	新建DefaultPackage用于存放原生文件（例如热更新dll）和ResourcesPackage 用于存放资源文件。
	包体需要设置可寻址Enalbe Addressable
2、	生成热更新dll
	使用HybridCLR工具生成热更新相关dll（目前是"hotupdate.dll", "mscorlib.dll", "System.dll", "System.Core.dll",）
	需要将以上dll放置YooAsset指定热更新文件内（HotUpdate/HotUpdateRes/Dll）
	热更新资源文件放置（HotUpdate/HotUpdateRes/Prefab）
3、	打包资源。
	打开YooAsset AssetBundleBuilder，
	原生文件需要原生文件构建管线。即DefaultPackage需要使用RawFileBuildPipeline
	资源文件需要 传统的内置构建管线。即和ResourcesPackage需要使用BuiltinBuildPipeline
4、	将打包出来的资源放置远端
5、 通用Launch.cs启动游戏流程（playMode选择HostPlayMode联机运行模式）
	