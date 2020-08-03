using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using Unity.Mathematics;

public class LayerManager : MonoBehaviour
{
    [SerializeField, Range(1, 200)] private float _Frecuency1, _Frecuency2;
    
    [SerializeField] private float2 _TimeScale;
    [SerializeField, Range(1, 10)] private int _Levels = 5;
    
    [SerializeField] private Color[] _Colors;

    [SerializeField] private GameObject _UIHolder;
    [SerializeField] private RawImage _TargetImagePrefab;

    // Compute Variables
    [SerializeField] private ComputeShader _Compute;
    ComputeBuffer _ColorBuffer;

    //
    [SerializeField, Range(0, 10)] private float _K = 0.5f;
    [SerializeField, Range(0, 10)] private float _W = 0.5f;

    private int _KernelIndex;
    private string _KernelName = "NoiseGenerator";

    private float2 _Offset1, _Offset2;
    private int _LevelsPriv;
    // Colors Saved
    private float3[] _ColorBackUp;

    // Texture Containers
    private RenderTexture _Layers;
    private RenderTexture[] _Aux;

    private int width = 960, height = 960;

    // Start is called before the first frame update
    void Start()
    {
        _LevelsPriv = _Levels;

        // All the layers un GPU memmory
        _Layers = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

        _Layers.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        _Layers.enableRandomWrite = true;
        _Layers.volumeDepth = _LevelsPriv;
        _Layers.Create();

        // Auxiliar textures to blit the results out of the array
        _Aux = new RenderTexture[_LevelsPriv];

        //Initting every texture
        for(int i = 0; i < _Aux.Length; i++)
        {
            _Aux[i] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _Aux[i].enableRandomWrite = true;
            _Aux[i].Create();
        }

        InitCompute();

        //Create the RawImages to store every layer
        for (int i = 0; i < _LevelsPriv; i++)
        {
            var obj = Instantiate(_TargetImagePrefab, _UIHolder.transform);
            obj.transform.localPosition = new float3(0.0f,i*15f,0.0f);
            obj.transform.localRotation = Quaternion.Euler(70, 0, -45);

            obj.name = "Tex: " + i;

            obj.texture = _Aux[i];
        }

        //_TargetImage.texture = _Layers;
    }

    // Update is called once per frame
    void Update()
    {
        _Offset1.y = Time.time * _TimeScale.x;
        _Offset2.x = Time.time * _TimeScale.y;


        SetNoiseParameters(_Frecuency1, _Frecuency2, _Offset1, _Offset2);

        _Compute.SetFloat("_Time", Time.time%120);
        _Compute.SetFloat("_K", _K);
        _Compute.SetFloat("_W", _W);

        _Compute.Dispatch(_KernelIndex, width / 8, height / 8, 1);

        for(int i = 0; i < _LevelsPriv; i++)
            Graphics.Blit(_Layers, _Aux[i], i, 0);
    }

    void InitCompute()
    {
        _KernelIndex = _Compute.FindKernel(_KernelName);
        _Compute.SetTexture(_KernelIndex, "_TexArray", _Layers);
        _Compute.SetInt("levels", _LevelsPriv);

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
