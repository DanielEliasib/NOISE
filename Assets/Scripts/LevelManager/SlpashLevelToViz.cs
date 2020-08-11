using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SlpashLevelToViz : MonoBehaviour
{
    [SerializeField] private Animator _Animator;
    [SerializeField] private int _MainSceneIndex = 0;
    private AsyncOperation _LoadOp = null;
    private Scene _CurrentScene;

    // Start is called before the first frame update
    void Start()
    {
        _LoadOp = SceneManager.LoadSceneAsync(_MainSceneIndex, LoadSceneMode.Additive);
        _CurrentScene = SceneManager.GetActiveScene();
    }

    // Update is called once per frame
    void Update()
    {
        if(_LoadOp != null)
        {
            if (_LoadOp.isDone)
            {
                _Animator.SetTrigger("TriggerTrans");
                
            }
        }
    }

    private void OnDestroy()
    {
        
    }

    public void OnFadeFinish()
    {
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(_MainSceneIndex));
        SceneManager.UnloadSceneAsync(_CurrentScene);
    }
}
