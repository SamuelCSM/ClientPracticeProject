using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class TestUniTask : MonoBehaviour
{
    public Button btn;
    public Text txt;

    private CancellationTokenSource cts;

    // Start is called before the first frame update
    void Start()
    {
        if (btn != null) 
            btn.onClick.AddListener(OnClick);
        Debug.LogError($"Start cur Frame:{Time.frameCount}");

        cts = new CancellationTokenSource();

        //UniTaskMgr.Instance.AddEveryDelayFrameTask(() => { Debug.LogError($"添加一个每帧执行的任务，frame:{Time.frameCount}"); }, 1, cts);
        Stopwatch sw = new Stopwatch();
        sw.Start();
        UniTaskMgr.Instance.AddEveryDelayTimeTask(() => { Debug.LogError($"添加一个每秒执行的任务，time:{sw.ElapsedMilliseconds / 1000}"); }, 1, cts);
    }

    private void OnClick()
    {
        Debug.LogError($">>>>>>>>> click cancel. cur frame : {Time.frameCount}");
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    void FixedUpdate()
    {
        //Debug.LogError($"******************************* FixedUpdate Frame:{Time.frameCount} *******************************");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.LogError($"******************************* Update Frame:{Time.frameCount} *******************************");
    }

    void LateUpdate()
    {
        //Debug.LogError($"******************************* LateUpdate Frame:{Time.frameCount} *******************************");
    }
}
