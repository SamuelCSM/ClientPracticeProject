using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class UniTaskMgr
{
    private static UniTaskMgr instance;

    public static UniTaskMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UniTaskMgr();
            }
            return instance;
        }
    }

    private UniTaskMgr()
    {
        // Private constructor to prevent instantiation from outside
    }

    #region add Cur Frame Task
    /// <summary>
    /// 添加一个任务到指定的PlayerLoopTiming
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_timing"> 游戏循环时间节点枚举 </param>
    /// <param name="_cancellationToken"> 取消任务token </param>
    /// <param name="_cancelImmediately"> 是否</param>
    /// <returns></returns>
    private async UniTask _createPlayerLoopTimingTask(Action _act, PlayerLoopTiming _timing, CancellationToken _cancellationToken, bool _cancelImmediately = false)
    {
        if (_act == null)
            return;

        await UniTask.Yield(_timing, _cancellationToken, _cancelImmediately);

        _act();
    }

    /// <summary>
    /// 添加一个任务到指定的PlayerLoopTiming  
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_timing"></param>
    /// <param name="_cancellationTokenSource"></param>
    /// <param name="_cancelImmediately"></param>
    /// <returns></returns>
    public async UniTask createPlayerLoopTimingTask(Action _act, PlayerLoopTiming _timing, CancellationTokenSource _cancellationTokenSource, bool _cancelImmediately = false)
    {
        if (_act == null || _cancellationTokenSource == null)
            return;

        try
        {
            await _createPlayerLoopTimingTask(_act, _timing, _cancellationTokenSource.Token, _cancelImmediately);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return;
            
            Debug.LogError(ex);
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public async UniTask createPlayerLoopTimingTask(Action _act, PlayerLoopTiming _timing, CancellationToken _cancellationToken = default, bool _cancelImmediately = false)
    {
        if (_act == null)
            return;

        try
        {
            await _createPlayerLoopTimingTask(_act, _timing, _cancellationToken, _cancelImmediately);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return;

            Debug.LogError(ex);
        }
    }

    /// <summary>
    /// 添加一个任务到FixedUpdate
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_cancellationTokenSource"></param>
    /// <returns></returns>
    public async UniTask createYieldFixedUpdateTask(Action _act, CancellationTokenSource _cancellationTokenSource)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.FixedUpdate, _cancellationTokenSource);
    }

    public async UniTask createYieldFixedUpdateTask(Action _act)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.FixedUpdate);
    }

    /// <summary>
    /// 添加一个任务到Update
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_cancellationTokenSource"></param>
    /// <returns></returns>
    public async UniTask createYieldUpdateTask(Action _act, CancellationTokenSource _cancellationTokenSource)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.Update, _cancellationTokenSource);
    }

    public async UniTask createYieldUpdateTask(Action _act)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.Update);
    }

    /// <summary>
    /// 添加一个任务到LateUpdate
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_cancellationTokenSource"></param>
    /// <returns></returns>
    public async UniTask createYieldLateUpdateTask(Action _act, CancellationTokenSource _cancellationTokenSource)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.LastUpdate, _cancellationTokenSource);
    }

    public async UniTask createYieldLateUpdateTask(Action _act)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.LastUpdate);
    }

    #endregion

    #region add Next Frame Task
    /// <summary>
    /// 添加一个任务到下一帧指定的PlayerLoopTiming
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_timing"></param>
    /// <param name="_cancellationToken"></param>
    /// <param name="_cancelImmediately"></param>
    /// <returns></returns>
    private async UniTask _createNextPlayerLoopTimingTask(Action _act, PlayerLoopTiming _timing, CancellationToken _cancellationToken, bool _cancelImmediately = false)
    {
        if (_act == null)
            return;

        await UniTask.NextFrame(_timing, _cancellationToken, _cancelImmediately);

        _act();
    }

    /// <summary>
    /// 添加一个任务到下一帧指定的PlayerLoopTiming
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_timing"></param>
    /// <param name="_cancellationTokenSource"></param>
    /// <param name="_cancelImmediately"></param>
    /// <returns></returns>
    public async UniTask createNextPlayerLoopTimingTask(Action _act, PlayerLoopTiming _timing, CancellationTokenSource _cancellationTokenSource, bool _cancelImmediately = false)
    {
        if (_act == null || _cancellationTokenSource == null)
            return;

        try
        {
            await _createNextPlayerLoopTimingTask(_act, _timing, _cancellationTokenSource.Token, _cancelImmediately);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return;

            Debug.LogError(ex);
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public async UniTask createNextPlayerLoopTimingTask(Action _act, PlayerLoopTiming _timing, CancellationToken _cancellationToken = default, bool _cancelImmediately = false)
    {
        if (_act == null)
            return;

        try
        {
            await _createNextPlayerLoopTimingTask(_act, _timing, _cancellationToken, _cancelImmediately);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return;

            Debug.LogError(ex);
        }
    }

    /// <summary>
    /// 添加一个任务到下一帧FixedUpdate
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_cancellationTokenSource"></param>
    /// <returns></returns>
    public async UniTask createYieldNextFixedUpdateTask(Action _act, CancellationTokenSource _cancellationTokenSource)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.FixedUpdate, _cancellationTokenSource);
    }

    public async UniTask createYieldNextFixedUpdateTask(Action _act)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.FixedUpdate);
    }

    /// <summary>
    /// 添加一个任务到下一帧Update
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_cancellationTokenSource"></param>
    /// <returns></returns>
    public async UniTask createYieldNextUpdateTask(Action _act, CancellationTokenSource _cancellationTokenSource)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.Update, _cancellationTokenSource);
    }

    public async UniTask createYieldNextUpdateTask(Action _act)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.Update);
    }

    /// <summary>
    /// 添加一个任务到下一帧LateUpdate
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_cancellationTokenSource"></param>
    /// <returns></returns>
    public async UniTask createYieldNextLateUpdateTask(Action _act, CancellationTokenSource _cancellationTokenSource)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.LastUpdate, _cancellationTokenSource);
    }

    public async UniTask createYieldNextLateUpdateTask(Action _act)
    {
        await createPlayerLoopTimingTask(_act, PlayerLoopTiming.LastUpdate);
    }
    #endregion


    public CancellationTokenSource createCancellationTokenSourceByTask(UniTask _uniTask)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        ToCancellationTokenCore(_uniTask, cts).Forget();
        return cts;
    }

    static async UniTaskVoid ToCancellationTokenCore(UniTask task, CancellationTokenSource cts)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return;

            Debug.LogError(ex);
        }
        cts.Cancel();
        cts.Dispose();
    }


    // 添加一个每帧执行的任务
    public async UniTask AddEveryUpdateTask(Action _act, CancellationTokenSource cts)
    {
        if (_act == null)
            return;

        // 在UniTask执行过程中检查取消请求
        while (cts == null || !cts.IsCancellationRequested)
        {
            _act();
            CancellationToken token = cts == null ? CancellationToken.None :  cts.Token;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    /// <summary>
    /// 添加一个每隔x秒执行的任务
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_delayTime"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public async UniTask AddEveryDelayTimeTask(Action _act, float _delayTime, CancellationTokenSource cts)
    {
        if (_act == null)
            return;

        // 在UniTask执行过程中检查取消请求
        while (cts == null || !cts.IsCancellationRequested)
        {
            _act();
            int delayTime = (int)(_delayTime * 1000);
            CancellationToken token = cts == null ? CancellationToken.None : cts.Token;
            await UniTask.Delay(delayTime, DelayType.Realtime, PlayerLoopTiming.Update, token);
        }
    }

    /// <summary>
    /// 添加一个每隔x帧执行的任务
    /// </summary>
    /// <param name="_act"></param>
    /// <param name="_delayFrameCount"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    public async UniTask AddEveryDelayFrameTask(Action _act, int _delayFrameCount, CancellationTokenSource cts)
    {
        if (_act == null)
            return;

        // 在UniTask执行过程中检查取消请求
        while (cts == null || !cts.IsCancellationRequested)
        {
            _act();
            CancellationToken token = cts == null ? CancellationToken.None : cts.Token;
            await UniTask.DelayFrame(_delayFrameCount, PlayerLoopTiming.Update, token);
        }
    }

}

