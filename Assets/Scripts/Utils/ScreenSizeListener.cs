using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSizeListener
{
    Vector2 _CurrentResolution;
    public List<Transform> _Layers;
    private float ratio = 0;
    private float separationRatio = 0;
    private WaitForSeconds _WaitXSeconds;
    private WaitForEndOfFrame _WaitEndOfFrame;

    public void Awake(MonoBehaviour mono)
    {
        _CurrentResolution = new Vector2(Screen.width, Screen.height);
        Debug.Log("Res: " + _CurrentResolution);
        ratio = 850.0f/ 1080.0f;
        separationRatio = 15f/1080.0f;
        //_WaitXSeconds = new WaitForSeconds(30);
        //_WaitEndOfFrame = new WaitForEndOfFrame();
        //mono.StartCoroutine(Resize());              //? Why do I resize every 30 seconds?
        //AdjustScale();
        
    }

    public void Update()
    {
        if(_CurrentResolution.x != Screen.width || 
            _CurrentResolution.y != Screen.height)
        {
            _CurrentResolution = new Vector2(Screen.width, Screen.height);
            Debug.Log("Res: " + _CurrentResolution);
            AdjustScale();
        }
    }

    public void AdjustScale()
    {
        var scale = ratio * _CurrentResolution.y;
        
        var scaleV3 = new Vector3(scale, scale, scale);
        var separation = separationRatio * _CurrentResolution.y;
        var offset = _Layers.Count * separation * 0.45f;
        int i = 0;
        foreach (var layer in _Layers)
        {
            layer.transform.localPosition = new Vector3(0.0f, i*separation - offset, _Layers.Count-i);
            layer.transform.localScale = scaleV3;
            i++;
        }
    }

    //IEnumerator Resize()
    //{
    //    //while (true)
    //    //{
    //    //    Screen.SetResolution(Screen.width-2, Screen.height, false, 60);
    //    //    Debug.Log("Resize");
    //    //    yield return _WaitEndOfFrame;
    //    //    Screen.SetResolution(Screen.width + 2, Screen.height, false, 60);
    //    //    yield return _WaitXSeconds;
    //    //}
    //}
}
