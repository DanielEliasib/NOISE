using System.Collections;
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
    [SerializeField, Range(1, 1000)] private float _Frecuency1, _Frecuency2;
    
    [SerializeField] private float2 _TimeScale;
    [SerializeField, Range(1, 20)] private int _Levels = 5;

    [SerializeField] private ColorTemplate[] _ColorSchemes;
    private int _CurrentColorIndex;

    [SerializeField] private GameObject _UIHolder;
    [SerializeField] private GameObject _TargetQuad;
    //[SerializeField] private RawImage _TargetImagePrefab;

    // Compute Variables
    //[SerializeField] private ComputeShader _LayerCompute;
    [SerializeField] private ComputeShader _NoiseCompute;

    ComputeBuffer _ColorBuffer;
    ComputeBuffer _WaveDataBuffer;
    ComputeBuffer _AFDataBuffer;
    //ComputeBuffer _NoiseDataBuffer;
    ComputeBuffer _AmpDataBuffer;

    private int _NoiseKernelIndex, _LayerKernelIndex;
    private string _NoiseKernelName = "NoiseLayerGenerator", _LayerKernelName = "LayerSeparation";

    private float2 _Offset1, _Offset2;
    private int _LevelsPriv;
    // Colors Saved
    private float3[] _ColorBackUp;

    // Texture Containers
    //private RenderTexture _Layers;
    //private RenderTexture[] _Aux;
    private RenderTexture _NoiseTex;

    private int width = 256, height = 256;

    private List<float4> _WaveData;
    private List<float4> _WaveDataBack;
    private List<float4> _AFData;
    private List<float4> _AFDataBack;
    private List<float> _AmpData;
    private List<float> _AmpDataBack;

    [SerializeField] private int _SpectrumRes = 250;

    private double _Cooldown;
    private bool _CooldownActive;

    private LoopbackCapture _Loopback;

    private List<float>[] _LongBandProms;
    private float[] _Peaks;

    List<BandData> _BandData;
    List<(DSPFilters, float)>[] _FilterData;

    private ScreenSizeListener _SizeListener;
    private List<Transform> _LayerContainer;

    private float _TextureRadius;

    //Public Info
    [SerializeField] public float[] _BeatMultipliers;
    [SerializeField] public float[] _LongProms;
    [SerializeField] public float[][] _Spectrum;

    #region Unity Callbacks

    private void Awake()
    {
        //_CurrentColor = _ColorSchemes[0];
        _CurrentColorIndex = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        _SizeListener = new ScreenSizeListener();
        _SizeListener.Awake(this);

        _LayerContainer = new List<Transform>();

        _CooldownActive = false;

        _TextureRadius = Mathf.Sqrt(width * width*0.25f + height * height*0.25f);

        Application.targetFrameRate = -1;

        _LevelsPriv = _Levels;

        // All the layers un GPU memmory
        //_Layers = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        //_Layers.filterMode = FilterMode.Bilinear;
        //_Layers.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        //_Layers.enableRandomWrite = true;
        //_Layers.volumeDepth = _LevelsPriv;
        //_Layers.Create();

        _NoiseTex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _NoiseTex.enableRandomWrite = true;
        _NoiseTex.Create();

        InitCompute();

        float step = (1.0f) / _LevelsPriv;
        ColorTemplate _CurrentColorScheme = _ColorSchemes[_CurrentColorIndex];

        //Create the RawImages to store every layer
        for (int i = 0; i < _LevelsPriv; i++)
        {
            var obj = Instantiate(_TargetQuad, _UIHolder.transform);
            obj.transform.localPosition = new float3(0.0f, i * 15f - 50, i* step * 0.01f);
            obj.transform.localRotation = Quaternion.Euler(70, 0, -45);
            

            obj.name = "Tex: " + i;

            int currentColorID = (int)(i * step * (_CurrentColorScheme.Colors.Length - 1));

            Color color = Color.Lerp(_CurrentColorScheme.Colors[currentColorID], _CurrentColorScheme.Colors[currentColorID + 1], i * step * _CurrentColorScheme.Colors.Length - currentColorID);

            var ren = obj.GetComponent<Renderer>();
            ren.material.SetTexture("Texture2D_357675f864c14116816d9c8d7bf4d02c", _NoiseTex);
            ren.material.SetColor("Color_b422946d4dd44b25973975f9d477332c", color);
            ren.material.SetFloat("Vector1_8d60240980eb4a2e9c785044f80c1bd2", (i-2)*step);

            _LayerContainer.Add(obj.transform);
        }

        _SizeListener._Layers = _LayerContainer;
        _SizeListener.AdjustScale();

        _WaveData = new List<float4>();
        _AFData = new List<float4>();
        _AmpData = new List<float>();

        _WaveDataBack = new List<float4>();
        _AFDataBack = new List<float4>();
        _AmpDataBack = new List<float>();
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

    void OnGUI()
    {
        if (Event.current.type.Equals(EventType.Repaint))
        {
            // for (int i = 0; i < _LevelsPriv; i++)
            // {
            //     GL.PushMatrix();
            //     GL.LoadProjectionMatrix(Camera.main.projectionMatrix);
                
            //     GL.modelview = Camera.main.worldToCameraMatrix * this.transform.localToWorldMatrix;
            //     Graphics.DrawTexture(new Rect(0, 0, i, 1), _Aux[i]);
            //     GL.PopMatrix();
            // }
            
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Blit last frame data in order to let the compute shader work for at least one frame.
        //for(int i = 0 ; i < _LevelsPriv; i++)
        //{
        //    Graphics.Blit(_Layers, _Aux[i], i, 0);
        //}

        _SizeListener.Update();

        _Offset1.y = Time.time * _TimeScale.x;
        _Offset2.x = Time.time * _TimeScale.y;

        ProcessWaveData();
        WaveBufferManager(_WaveData.Count);

        SetNoiseParameters(_Frecuency1, _Frecuency2, _Offset1, _Offset2);

        DispatchNoiseShaders();
        
        //_LayerCompute.Dispatch(_NoiseKernelIndex, (width/2) / 8, (height/2) / 8, _Levels%4==0?_Levels/4 : _Levels / 4 + 1);

        if (_CooldownActive)
            _Cooldown -= Time.deltaTime;

        ProcessAudio();

        if (_Cooldown <= 0)
            _CooldownActive = false;

        Debug.Log("Number of waves: " + _WaveData.Count);
    }

    private void OnDestroy()
    {
        _Loopback.StopListening();

        try
        {
            _AFDataBuffer.Dispose();
            _WaveDataBuffer.Dispose();
            _ColorBuffer.Dispose();
            _AmpDataBuffer.Dispose();
        }
        catch { }

    }

    #endregion

    #region Compute Functions

    void InitCompute()
    {
        //_LayerKernelIndex = _LayerCompute.FindKernel(_LayerKernelName);
        _NoiseKernelIndex = _NoiseCompute.FindKernel(_NoiseKernelName);

        //_LayerCompute.SetTexture(_LayerKernelIndex, "Layers", _Layers);
        //_LayerCompute.SetInt("levels", _LevelsPriv);

        //_LayerCompute.SetInt("_TexWidth", width);
        //_LayerCompute.SetInt("_TexHeight", height);

        _NoiseCompute.SetInt("_TexWidth", width);
        _NoiseCompute.SetInt("_TexHeight", height);

        SetComputeColors(_ColorSchemes[0]);

        _WaveDataBuffer = new ComputeBuffer(25, sizeof(float) * 4);
        _AFDataBuffer = new ComputeBuffer(25, sizeof(float) * 4);
        _AmpDataBuffer = new ComputeBuffer(25, sizeof(float));

        //_LayerCompute.SetBuffer(_LayerKernelIndex, "_NoiseLayer", _NoiseDataBuffer);

        //! Noise
        _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_WaveData", _WaveDataBuffer);
        _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_AFData", _AFDataBuffer);
        _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_Amplituds", _AmpDataBuffer);
        _NoiseCompute.SetTexture(_NoiseKernelIndex, "_NoiseData", _NoiseTex);
        _NoiseCompute.SetInt("_TexWidth", width);
    }

    void WaveBufferManager(int numberOfWaves)
    {
        if (numberOfWaves > 0)
        {
            if (_WaveData.Count > _WaveDataBuffer.count)
            {
                _WaveDataBuffer.Dispose();
                _AFDataBuffer.Dispose();
                _AmpDataBuffer.Dispose();

                _WaveDataBuffer = new ComputeBuffer(numberOfWaves, sizeof(float) * 4);
                _AFDataBuffer = new ComputeBuffer(numberOfWaves, sizeof(float) * 4);
                _AmpDataBuffer = new ComputeBuffer(numberOfWaves, sizeof(float));

                _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_WaveData", _WaveDataBuffer);
                _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_AFData", _AFDataBuffer);
                _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_Amplituds", _AmpDataBuffer);

                Debug.Log("Number of waves: " + numberOfWaves);
            }

            _WaveDataBuffer.SetData(_WaveData);
            _AFDataBuffer.SetData(_AFData);
            _AmpDataBuffer.SetData(_AmpData);
        }

        _NoiseCompute.SetInt("numberOfWaves", numberOfWaves);
    }


    void SetNoiseParameters(float frecuency1, float frecuency2, float2 offset1, float2 offset2)
    {
        _NoiseCompute.SetFloat("_Time", Time.time);

        _NoiseCompute.SetFloat("_Freq1", frecuency1);
        _NoiseCompute.SetFloat("_Freq2", frecuency2);

        _NoiseCompute.SetFloats("_Offset1", new float[] { offset1.x, offset1.y });
        _NoiseCompute.SetFloats("_Offset2", new float[] { offset2.x, offset2.y });
    }

    void DispatchNoiseShaders()
    {
        SetNoiseParameters(_Frecuency1, _Frecuency2, _Offset1, _Offset2);
        _NoiseCompute.Dispatch(_NoiseKernelIndex, width / 8, height / 8, 1);
    }

    #endregion

    #region Wave Processing
    void CreateWave(float w, float k, float amp, float decay)
    {
        _WaveData.Add(new float4(w / k, 2 * Mathf.PI / k, k, w));
        _AFData.Add(new float4(amp, decay, w / (2 * Mathf.PI), Time.time));
        _AmpDataBack.Add(amp);
        _CooldownActive = true;
    }

    void ProcessWaveData()
    {
        _AFDataBack.Clear();
        _WaveDataBack.Clear();
        _AmpDataBack.Clear();

        for(int i = 0; i < _WaveData.Count; i++)
        {
            //Check if the wave already leaved the screen, if so it removes it from the data
            if(!(_WaveData[i].x*(Time.time - _AFData[i].w) - _WaveData[i].y*0.5f > _TextureRadius))
            {
                _WaveDataBack.Add(_WaveData[i]);
                _AFDataBack.Add(_AFData[i]);
                // Pre calculate amplituds
                _AmpDataBack.Add(_AFData[i].x/Mathf.Exp(_AFData[i].y * (Time.time - _AFData[i].w)));
            }
        }

        _WaveData = new List<float4>(_WaveDataBack);
        _AFData = new List<float4>(_AFDataBack);
        _AmpData = new List<float>(_AmpDataBack);
    }
    #endregion

    #region Audio Analysis
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
                            CreateWave(10f, 0.06f, 0.25f, 2.2f);
                            break;
                        case 1:
                            CreateWave(15f, 0.065f, 0.2f, 2.3f);
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
    #endregion

    public void NextColorScheme()
    {
        _CurrentColorIndex = (_CurrentColorIndex + 1) % _ColorSchemes.Length;
        SetComputeColors(_ColorSchemes[_CurrentColorIndex]);
    }

    void SetComputeColors(ColorTemplate current)
    {
        //! Colors
        _ColorBuffer = new ComputeBuffer(current.Colors.Length, 3 * sizeof(float));

        _ColorBackUp = new float3[current.Colors.Length];

        for (int i = 0; i < _ColorBackUp.Length; i++)
            _ColorBackUp[i] = new float3(current.Colors[i].r, current.Colors[i].g, current.Colors[i].b);

        _ColorBuffer.SetData(_ColorBackUp);
        //_LayerCompute.SetBuffer(_LayerKernelIndex, "_Colors", _ColorBuffer);
        //_LayerCompute.SetInt("numberOfColors", current.Colors.Length);
    }

}
