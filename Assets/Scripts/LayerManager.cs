using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class LayerManager : MonoBehaviour
{
    private RenderTexture _Layer;
    [SerializeField] private RawImage _TargetImage;
    [SerializeField] private ComputeShader _Compute;

    private int _KernelIndex;
    private string _KernelName = "NoiseGenerator";

    private int width = 640, height = 640;
    [SerializeField, Range(1, 200)] private float _Frecuency;
    [SerializeField] private int _XOffset = 0, _YOffset = 0; 

    // Start is called before the first frame update
    void Start()
    {
        _Layer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _Layer.enableRandomWrite = true;
        _Layer.Create();

        

        InitCompute();

        
        _TargetImage.texture = _Layer;
    }

    // Update is called once per frame
    void Update()
    {
        
        _Frecuency = Mathf.Lerp(18, 22, Mathf.Sin(Time.realtimeSinceStartup));
        SetNoiseParameters(_Frecuency, width, height, _XOffset* Mathf.Sin(Time.realtimeSinceStartup), _YOffset* Mathf.Cos(Time.realtimeSinceStartup));
        _Compute.Dispatch(_KernelIndex, width / 8, height / 8, 1);
    }

    void InitCompute()
    {
        _KernelIndex = _Compute.FindKernel(_KernelName);

        _Compute.SetTexture(_KernelIndex, "Result", _Layer);
    }

    void SetNoiseParameters(float frecuency, float width, float height, float xOffset, float yOffset) 
    {
        _Compute.SetFloat("_Frecuency", frecuency);
        _Compute.SetFloat("_SizeX", width);
        _Compute.SetFloat("_SizeY", height);
        _Compute.SetFloat("_OffsetX", xOffset);
        _Compute.SetFloat("_OffsetY", yOffset);
    }
}
