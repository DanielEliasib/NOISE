using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using Unity.Mathematics;

public class LayerManager : MonoBehaviour
{
    private RenderTexture _Layers;
    private RenderTexture[] _Aux;
    [SerializeField] private RawImage _TargetImage;
    [SerializeField] private ComputeShader _Compute;

    private int _KernelIndex;
    private string _KernelName = "NoiseGenerator";

    private int width = 512, height = 512;

    [SerializeField, Range(1, 200)] private float _Frecuency1, _Frecuency2;
    [SerializeField] private float2 _Offset1, _Offset2;
    [SerializeField] private float2 _TimeScale;
    [SerializeField, Range(1, 10)] private int _Levels = 5;
    [SerializeField, Range(1, 10)] private int _CurrentLayer = 1;
    [SerializeField] private Color[] _Colors;
    [SerializeField] private GameObject _UIHolder;

    ComputeBuffer _ColorBuffer;
    private float3[] _ColorBackUp;
    // Start is called before the first frame update
    void Start()
    {
        _Layers = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _Aux = new RenderTexture[_Levels];
        for(int i = 0; i < _Aux.Length; i++)
        {
            _Aux[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _Aux[i].enableRandomWrite = true;
            _Aux[i].Create();
        }
            
        //_Aux = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

        _Layers.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        _Layers.enableRandomWrite = true;
        _Layers.volumeDepth = _Levels;
        _Layers.Create();

        InitCompute();


        for (int i = 0; i < _Levels; i++)
        {
            var obj = GameObject.Instantiate(_TargetImage, _UIHolder.transform);
            obj.transform.localPosition = new float3(0.0f,i*15f,0.0f);
            obj.transform.rotation = Quaternion.Euler(70, 0, -45);

            obj.texture = _Aux[i];
        }

        //_TargetImage.texture = _Layers;
    }

    // Update is called once per frame
    void Update()
    {
        FixCurrentLayer();

        //_Frecuency = Mathf.Lerp(18, 22, Mathf.Sin(Time.realtimeSinceStartup));

        //_Offset1 += new float2((Time.time % 100) *0.01f, 0.0f);
        //_Offset2 += new float2(0.0f, (Time.time % 100) * 0.01f);

        _Offset1.y = Time.time * _TimeScale.x;
        _Offset2.x = Time.time * _TimeScale.y;


        SetNoiseParameters(_Frecuency1, _Frecuency2, _Offset1, _Offset2);
        _Compute.Dispatch(_KernelIndex, width / 8, height / 8, 1);

        for(int i = 0; i < _Levels; i++)
            Graphics.Blit(_Layers, _Aux[_Levels - i - 1], i, 0);
    }

    void FixCurrentLayer()
    {
        _CurrentLayer = Mathf.Clamp(_CurrentLayer, 0, _Levels);
    }

    void InitCompute()
    {
        _KernelIndex = _Compute.FindKernel(_KernelName);
        _Compute.SetTexture(_KernelIndex, "_TexArray", _Layers);
        _Compute.SetInt("levels", _Levels);

        _ColorBuffer = new ComputeBuffer(_Colors.Length,3*sizeof(float));

        _ColorBackUp = new float3[_Colors.Length];

        for (int i = 0; i < _ColorBackUp.Length; i++)
            _ColorBackUp[i] = new float3(_Colors[i].r, _Colors[i].g, _Colors[i].b);

        _ColorBuffer.SetData(_ColorBackUp);

        _Compute.SetBuffer(_KernelIndex, "_Colors", _ColorBuffer);
        _Compute.SetInt("numberOfColors", _Colors.Length);
    }

    void SetNoiseParameters(float frecuency1, float frecuency2, float2 offset1, float2 offset2) 
    {
        _Compute.SetFloat("_Frecuency1", frecuency1);
        _Compute.SetFloat("_Frecuency2", frecuency2);

        _Compute.SetFloats("_Offset1", new float[] { offset1.x, offset1.y });
        _Compute.SetFloats("_Offset2", new float[] { offset2.x, offset2.y });
    }
}
