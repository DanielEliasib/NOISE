using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using Unity.Mathematics;
using CSCore.DMO;

public class LayerManager : MonoBehaviour
{
    [SerializeField, Range(1, 200)] private float _Frecuency1, _Frecuency2;
    
    [SerializeField] private float2 _TimeScale;
    [SerializeField, Range(1, 20)] private int _Levels = 5;
    
    [SerializeField] private Color[] _Colors;

    [SerializeField] private GameObject _UIHolder;
    [SerializeField] private RawImage _TargetImagePrefab;

    // Compute Variables
    [SerializeField] private ComputeShader _Compute;
    ComputeBuffer _ColorBuffer;
    ComputeBuffer _WaveDataBuffer;
    ComputeBuffer _AFDataBuffer;

    //
    [SerializeField, Range(0, 0.1f)] private float _K = 0.1f;
    [SerializeField, Range(0, 100)] private float _W = 0.5f;

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

    private List<float4> _WaveData;
    private List<float4> _WaveDataBack;
    private List<float4> _AFData;
    private List<float4> _AFDataBack;

    private LoopbackListener _Listener;

    private float[] _Spectrum;

    private int _MinSpectrumIndex;
    private int _MaxSpectrumIndex;
    private int _SpectrumRes = 120;

    private double _Cooldown;
    private bool _CooldownActive;

    [SerializeField, Range(0.0f, 1.0f)] private float _MinRange, _MaxRange;

    [SerializeField, Range(0, 20)] private float _Disc = 5.0f;
    [SerializeField, Range(0, 0.1f)] private float _Delay = 0.03f;

    void Awake()
    {
        _Listener = new LoopbackListener(_SpectrumRes, Assets.Scripts.Audio.ScalingStrategy.Sqrt, 0.8f, 0.5f, 1.2f, 1.5f);
        _MinSpectrumIndex = (int)(_SpectrumRes * _MinRange);
        _MaxSpectrumIndex = (int)(_SpectrumRes * _MaxRange);

        _CooldownActive = false;
    }

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

        _WaveData = new List<float4>();
        _AFData = new List<float4>();
        _WaveDataBack = new List<float4>();
        _AFDataBack = new List<float4>();
        //_TargetImage.texture = _Layers;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _Offset1.y = Time.time * _TimeScale.x;
        _Offset2.x = Time.time * _TimeScale.y;

        ProcessInput();
        ProcessWaves();
        WaveBufferManager(_WaveData.Count);

        SetNoiseParameters(_Frecuency1, _Frecuency2, _Offset1, _Offset2);

        _Compute.SetFloat("_Time", Time.time);

        _Compute.Dispatch(_KernelIndex, width / 8, height / 8, 1);

        for(int i = 0; i < _LevelsPriv; i++)
            Graphics.Blit(_Layers, _Aux[i], i, 0);

        if (_CooldownActive)
            _Cooldown -= Time.deltaTime;

        ProcessAudio();

        if (_Cooldown <= 0)
            _CooldownActive = false;
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

        _WaveDataBuffer = new ComputeBuffer(1, sizeof(float) * 4);
        _AFDataBuffer = new ComputeBuffer(1, sizeof(float) * 4);
    }

    void WaveBufferManager(int numberOfWaves)
    {
        try
        {
            _WaveDataBuffer.Dispose();
            _AFDataBuffer.Dispose();
        }
        catch { }

        if(numberOfWaves > 0)
        {
            _WaveDataBuffer = new ComputeBuffer(numberOfWaves, sizeof(float) * 4);
            _AFDataBuffer = new ComputeBuffer(numberOfWaves, sizeof(float) * 4);

            _Compute.SetBuffer(_KernelIndex, "_WaveData", _WaveDataBuffer);
            _Compute.SetBuffer(_KernelIndex, "_AFData", _AFDataBuffer);

            _WaveDataBuffer.SetData(_WaveData);
            _AFDataBuffer.SetData(_AFData);
        }
        else
        {
            _WaveDataBuffer = new ComputeBuffer(1, sizeof(float) * 4);
            _AFDataBuffer = new ComputeBuffer(1, sizeof(float) * 4);

            _Compute.SetBuffer(_KernelIndex, "_WaveData", _WaveDataBuffer);
            _Compute.SetBuffer(_KernelIndex, "_AFData", _AFDataBuffer);

        }

        _Compute.SetInt("numberOfWaves", numberOfWaves);
    }


    void SetNoiseParameters(float frecuency1, float frecuency2, float2 offset1, float2 offset2) 
    {
        _Compute.SetFloat("_Frecuency1", frecuency1);
        _Compute.SetFloat("_Frecuency2", frecuency2);

        _Compute.SetFloats("_Offset1", new float[] { offset1.x, offset1.y });
        _Compute.SetFloats("_Offset2", new float[] { offset2.x, offset2.y });
    }

    void ProcessInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
            float w = _W, k = _K;
            _WaveData.Add(new float4(w/k, 2 * Mathf.PI/k, k, w));
            _AFData.Add(new float4(0.5f, 2.5f, w/(2 * Mathf.PI),Time.time));

            Debug.Log("Wave added: " + _WaveData[_WaveData.Count - 1] + "\n" + _AFData[_AFData.Count - 1]);
        }
    }

    void CreateWave()
    {
        float w = _W, k = _K;
        _WaveData.Add(new float4(w / k, 2 * Mathf.PI / k, k, w));
        _AFData.Add(new float4(0.35f, 2.5f, w / (2 * Mathf.PI), Time.time));

    }

    void ProcessWaves()
    {
        _AFDataBack.Clear();
        _WaveDataBack.Clear();

        for(int i = 0; i < _WaveData.Count; i++)
        {
            //Check if the wave already leaved the screen, if so it removes it from the data
            if(!(_WaveData[i].x*(Time.time - _AFData[i].w) + _WaveData[i].y > Mathf.Sqrt(width*width + height * height)))
            {
                _WaveDataBack.Add(_WaveData[i]);
                _AFDataBack.Add(_AFData[i]);
            }

        }

        _WaveData = new List<float4>(_WaveDataBack);
        _AFData = new List<float4>(_AFDataBack);
    }

    private void ProcessAudio()
    {
        if (!_CooldownActive)
        {
            _Spectrum = _Listener.SpectrumData;
            float max = 0;
            float prom = 0;
            int count = 0;

            _MinSpectrumIndex = (int)(_SpectrumRes * _MinRange);
            _MaxSpectrumIndex = (int)(_SpectrumRes * _MaxRange);

            for (int i = _MinSpectrumIndex; i <= _MaxSpectrumIndex; i++)
            {
                if (_Spectrum[i] > max)
                    max = _Spectrum[i];

                prom += _Spectrum[i];

                count++;

                if (_Spectrum[i] > _Disc)
                {
                    CreateWave();
                    _Cooldown = _Delay;
                    _CooldownActive = true;
                    break;
                }

            }

            prom = prom / count;
            Debug.Log("Promedy: " + prom + "\nMax: " + max);
        }
    }

    private void OnDestroy()
    {
        try
        {
            _ColorBuffer.Dispose();
            _AFDataBuffer.Dispose();
            _WaveDataBuffer.Dispose();
        }
        catch { }
        
    }
}
