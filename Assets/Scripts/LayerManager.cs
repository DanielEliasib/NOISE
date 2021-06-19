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
    [Header("Noise parameters")]
    [SerializeField, Range(1, 1000)] private float _Frecuency1, _Frecuency2;
    
    [SerializeField] private float2 _TimeScale;
    [SerializeField] private int _SpectrumRes = 250;

    [Header("Layer parameters")]
    [SerializeField, Range(1, 20)] private int _Levels = 5;

    [Header("Color parameters")]
    [SerializeField] private ColorTemplate[] _ColorSchemes;
    private int _CurrentColorIndex;

    [Header("Layer Spawning")]
    [SerializeField] private GameObject _UIHolder;
    [SerializeField] private GameObject _TargetQuad;

    [Header("Noise compute")]
    [SerializeField] private ComputeShader _NoiseCompute;

    private float2 _Offset1, _Offset2;
    private int _LevelsPriv;

    // Texture Containers
    private RenderTexture _NoiseTex;

    private int width = 256, height = 256;

    private List<float4> _WaveData;
    private List<float4> _AFData;
    private List<float> _AmpData;

    AudioAnalyser _AudioAnalyser;
    WaveProcessor _WaveProcessor;

    List<BandData> _BandData;
    List<(DSPFilters, float)>[] _FilterData;

    private ScreenSizeListener _SizeListener;
    private List<Transform> _LayerContainer;

    //Public Info
    float[] _BeatMultipliers;
    float[] _LongProms;
    float[][] _Spectrum;

    #region Unity Callbacks

    private void Awake()
    {
        //_CurrentColor = _ColorSchemes[0];
        _CurrentColorIndex = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = -1;

        //! ------------------------------------------
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

        _AudioAnalyser = new AudioAnalyser(_BandData, _FilterData, _SpectrumRes, 67);

        _AudioAnalyser.BindWaveData(out _WaveData, out _AFData, out _AmpData);
        _AudioAnalyser.BindArrayData(out _BeatMultipliers, out _LongProms, out _Spectrum);

        //! ------------------------------------------
        _WaveProcessor = new WaveProcessor(_NoiseCompute, new int2(width, height), new float2(_Frecuency1, _Frecuency2));
        _WaveProcessor.BindWaveData(in _WaveData, in _AFData, in _AmpData, out _NoiseTex);

        //! ------------------------------------------
        _SizeListener = new ScreenSizeListener();
        _SizeListener.Awake(this);

        //! ------------------------------------------
        _LayerContainer = new List<Transform>();

        _LevelsPriv = _Levels;

        float step = (1.0f) / _LevelsPriv;
        ColorTemplate _CurrentColorScheme = _ColorSchemes[_CurrentColorIndex];

        //Create the RawImages to store every layer
        for (int i = 0; i < _LevelsPriv; i++)
        {
            var obj = Instantiate(_TargetQuad, _UIHolder.transform);
            //obj.transform.localPosition = new float3(0.0f, i * 15f - 75, i* step * 0.005f);
            obj.transform.localRotation = Quaternion.Euler(70, 0, -45);
            

            obj.name = "Tex: " + i;

            int currentColorID = (int)(i * step * (_CurrentColorScheme.Colors.Length - 1));

            Color color = Color.Lerp(_CurrentColorScheme.Colors[currentColorID], _CurrentColorScheme.Colors[currentColorID + 1], i * step * _CurrentColorScheme.Colors.Length - currentColorID);

            var ren = obj.GetComponent<Renderer>();
            ren.material.SetTexture("Texture2D_357675f864c14116816d9c8d7bf4d02c", _NoiseTex);
            ren.material.SetColor("Color_b422946d4dd44b25973975f9d477332c", color);
            ren.material.SetFloat("Vector1_8d60240980eb4a2e9c785044f80c1bd2", (i+1)*step);

            _LayerContainer.Add(obj.transform);
        }

        //! ------------------------------------------
        _SizeListener._Layers = _LayerContainer;
        _SizeListener.AdjustScale();

        _Offset1 += new float2(UnityEngine.Random.Range(-300,300), UnityEngine.Random.Range(-300, 300));
        _Offset2 += new float2(UnityEngine.Random.Range(-300,300), UnityEngine.Random.Range(-300, 300));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _SizeListener.Update();

        _WaveProcessor.Update(_TimeScale, Time.time);
        _AudioAnalyser.ProcessAudio(Time.deltaTime);    //: Find a way to parallelize this

        Debug.Log("Number of waves: " + _WaveData.Count);
    }

    private void OnDestroy()
    {
        _AudioAnalyser.Destroy();

        _WaveProcessor.Dispose();
    }

    #endregion

    public void NextColorScheme()
    {
        _CurrentColorIndex = (_CurrentColorIndex + 1) % _ColorSchemes.Length;
        SetColorScheme();
    }

    public void BindArrayData(out float[] beatMultiplier, out float[] longProms, out float[][] spectrum)
    {
        beatMultiplier = _BeatMultipliers;
        longProms = _LongProms;
        spectrum = _Spectrum;
    }

    public void SetColorScheme()
    {
        float step = (1.0f) / _LevelsPriv;

        ColorTemplate _CurrentColorScheme = _ColorSchemes[_CurrentColorIndex];

        for (int i = 0; i < _LayerContainer.Count; i++)
        {
            var ren = _LayerContainer[i].GetComponent<Renderer>();

            int currentColorID = (int)(i * step * (_CurrentColorScheme.Colors.Length - 1));

            Color color = Color.Lerp(_CurrentColorScheme.Colors[currentColorID], _CurrentColorScheme.Colors[currentColorID + 1], i * step * _CurrentColorScheme.Colors.Length - currentColorID);

            ren.material.SetColor("Color_b422946d4dd44b25973975f9d477332c", color);
        }
    }

}
