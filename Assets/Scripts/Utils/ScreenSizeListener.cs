using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSizeListener
{
    Vector2 _CurrentResolution;
    public List<RawImage> _Layers;
    private float ratio = 0;
    private float separationRatio = 0;

    public void Awake()
    {
        _CurrentResolution = new Vector2(Screen.width, Screen.height);
        Debug.Log("Res: " + _CurrentResolution);
        ratio = 10.0f/ 1080.0f;
        separationRatio = 15f/1080.0f;
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
            layer.transform.localPosition = new Vector3(0.0f, i*separation - offset, 0.0f);
            layer.rectTransform.localScale = scaleV3;
            i++;
        }
    }
}
