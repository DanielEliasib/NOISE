﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using Unity.Mathematics;
using CSCore.DMO;

using AL.AudioSystem;
using UnityEngine.SceneManagement;
using System.Linq;

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
     
    private int _KernelIndex;
    private string _KernelName = "NoiseGenerator";

    private float2 _Offset1, _Offset2;
    private int _LevelsPriv;
    // Colors Saved
    private float3[] _ColorBackUp;

    // Texture Containers
    private RenderTexture _Layers;
    private RenderTexture[] _Aux;

    private int width = 512, height = 512;

    private List<float4> _WaveData;
    private List<float4> _WaveDataBack;
    private List<float4> _AFData;
    private List<float4> _AFDataBack;

    [SerializeField] private int _SpectrumRes = 250;

    private double _Cooldown;
    private bool _CooldownActive;

    private LoopbackCapture _Loopback;

    private List<float>[] _LongBandProms;
    private float[] _Peaks;

    List<BandData> _BandData;
    List<(DSPFilters, float)>[] _FilterData;

    private ScreenSizeListener _SizeListener;
    private List<RawImage> _LayerContainer;


    //Public Info
    [SerializeField] public float[] _BeatMultipliers;
    [SerializeField] public float[] _LongProms;
    [SerializeField] public float[][] _Spectrum;

    void Awake()
    {
        _SizeListener = new ScreenSizeListener();
        _SizeListener.Awake();

        _LayerContainer = new List<RawImage>();

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
            _Aux[i].filterMode = FilterMode.Bilinear;
            
            _Aux[i].enableRandomWrite = true;
            _Aux[i].Create();
        }

        InitCompute();

        //Create the RawImages to store every layer
        for (int i = 0; i < _LevelsPriv; i++)
        {
            var obj = Instantiate(_TargetImagePrefab, _UIHolder.transform);
            obj.transform.localPosition = new float3(0.0f, i * 15f - 50, 0.0f);
            obj.transform.localRotation = Quaternion.Euler(70, 0, -45);

            obj.name = "Tex: " + i;

            obj.texture = _Aux[i];

            _LayerContainer.Add(obj);
        }

        _SizeListener._Layers = _LayerContainer;
        _SizeListener.AdjustScale();

        _WaveData = new List<float4>();
        _AFData = new List<float4>();
        _WaveDataBack = new List<float4>();
        _AFDataBack = new List<float4>();
        //_TargetImage.texture = _Layers;

        //Loopback objects
        _BandData = new List<BandData>()
        {
            new BandData()
            {
                _minimumFrequency = 60,
                _maximumFrequency = 150
            },
            new BandData()
            {
                _minimumFrequency = 150,
                _maximumFrequency = 300
            }
        };

        _FilterData = new List<(DSPFilters, float)>[]
        {
            new List<(DSPFilters, float)>()
            {
                (DSPFilters.LowPass, 150)
            },
            new List<(DSPFilters, float)>()
            {
                (DSPFilters.LowPass, 300)
            }
        };

        _Loopback = new LoopbackCapture(_SpectrumRes, ScalingStrategy.Sqrt, _BandData, _FilterData);

        //  Operations are done in the fixed update. Set to 0.015 seconds, so 67 frames is about 1 second.
        _Loopback.StartListening();

        _LongBandProms = new List<float>[_BandData.Count];
        _BeatMultipliers = new float[_BandData.Count];
        _Peaks = new float[_BandData.Count];
        _LongProms = new float[_BandData.Count];

        for (int i = 0; i < _LongBandProms.Length; i++)
        {
            _LongBandProms[i] = new List<float>(67);
            _BeatMultipliers[i] = 1.5f;
            _Peaks[i] = float.MinValue;
        }
            

        _Offset1 += new float2(UnityEngine.Random.Range(-300,300), UnityEngine.Random.Range(-300, 300));
        _Offset2 += new float2(UnityEngine.Random.Range(-300,300), UnityEngine.Random.Range(-300, 300));

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _SizeListener.Update();

        _Offset1.y = Time.time * _TimeScale.x;
        _Offset2.x = Time.time * _TimeScale.y;

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

    void CreateWave(float w, float k, float amp, float decay)
    {
        _WaveData.Add(new float4(w / k, 2 * Mathf.PI / k, k, w));
        _AFData.Add(new float4(amp, decay, w / (2 * Mathf.PI), Time.time));
        _CooldownActive = true;
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
        _Spectrum = _Loopback.SpectrumData;

        float[] _BandProms = new float[_Spectrum.Length];

        for (int i = 0; i < _Spectrum.Length; i++)
        {
            _Peaks[i] = float.MinValue;

            if (_Spectrum[i] != null)
            {
                for (int j = 0; j < _Spectrum[i].Length; j++)
                {
                    _BandProms[i] += _Spectrum[i][j];

                    //Find Peaks
                    if(j <= 0)
                    {
                        if (_Spectrum[i][j] > _Spectrum[i][j+1])
                            _Peaks[i] = _Spectrum[i][j];
                    }
                    else if (j >= _Spectrum.Length - 1)
                    {
                        if (_Spectrum[i][j] > _Spectrum[i][j-1])
                            _Peaks[i] = _Spectrum[i][j];
                    }
                    else
                    {
                        if(_Spectrum[i][j] > _Spectrum[i][j - 1] && _Spectrum[i][j] > _Spectrum[i][j + 1])
                            _Peaks[i] = _Spectrum[i][j];
                    }

                }

                _BandProms[i] = _Spectrum[i].Length <= 0 ? 0 : _BandProms[i] / _Spectrum[i].Length;


            }
        }

        if (!_CooldownActive)
        {
            for(int i = 0; i < _LongBandProms.Length; i++)
            {
                var timeProm = 0.0f;

                foreach(var prom in _LongBandProms[i])
                {
                    timeProm += prom;
                }

                timeProm = _LongBandProms[i].Count <= 0 ? 0 : timeProm / _LongBandProms[i].Count;

                _LongProms[i] = timeProm;
                
                if (_Peaks[i] >= Mathf.Min(_BeatMultipliers[i] * timeProm, timeProm + 1) && _BandProms[i] >= 0.25f)
                {
                    switch (i)
                    {
                        case 0:
                            CreateWave(5.0f, 0.03f, 0.25f, 2);
                            break;
                        case 1:
                            CreateWave(7.5f, 0.03f, 0.20f, 5);
                            break;
                        default:
                            break;
                    }
                    _Cooldown = 0.015 * 5;
                }
            }
        }

        for(int i = 0; i < _LongBandProms.Length; i++)
        {
            if (_LongBandProms[i].Count >= _LongBandProms[i].Capacity && _LongBandProms[i].Capacity > 0)
                _LongBandProms[i].RemoveAt(0);

            _LongBandProms[i].Add(_BandProms[i]);
        }

            
    }

    private void OnDestroy()
    {
        _Loopback.StopListening();

        try
        {
            _AFDataBuffer.Dispose();
            _WaveDataBuffer.Dispose();
            _ColorBuffer.Dispose();
        }
        catch { }
        
    }
}
