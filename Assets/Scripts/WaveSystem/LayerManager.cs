using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using Unity.Mathematics;
using CSCore.DMO;

using AL.AudioSystem;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Serialization;

public class LayerManager : MonoBehaviour
{
    [Header("Noise parameters")]
    [SerializeField, Range(1, 5000)] private float _Frequency1;
    [SerializeField, Range(1, 5000)] private float _Frequency2;

    [SerializeField] private float2 _TimeScale;
    [SerializeField] private int _SpectrumRes = 250;

    [Header("Layer parameters")]
    [SerializeField, Range(1, 50)] private int _Levels = 5;

    [Header("Color parameters")]
    [SerializeField] private ColorTemplate[] _ColorSchemes;
    private int _CurrentColorIndex;

    [Header("Layer Spawning")]
    [SerializeField] private GameObject _UIHolder;
    [SerializeField] private GameObject _TargetQuad;

    [Header("Noise compute")]
    [SerializeField] private ComputeShader _NoiseCompute;

    //! --------------------------------------------------------------
    private float2 _Offset1, _Offset2;
    private int _nLevels;

    // Texture Containers
    private RenderTexture _NoiseTex;

    private const int texWidth = 256*5;
    private const int texHeight = 256*5;
    
    private WaveProcessor _waveProcessor;
    private WaveBaker _waveBaker;

    private ScreenSizeListener _SizeListener;
    private List<Transform> _LayerContainer;
    
    #region Unity Callbacks
    private void Awake()
    {
        _CurrentColorIndex = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = -1;
        
        InitializeLoopback();
        GenerateLayers();

        _SizeListener = new ScreenSizeListener();
        _SizeListener.Awake(this);
        
        _SizeListener._Layers = _LayerContainer;
        _SizeListener.AdjustScale();

        _Offset1 += new float2(UnityEngine.Random.Range(-300,300), UnityEngine.Random.Range(-300, 300));
        _Offset2 += new float2(UnityEngine.Random.Range(-300,300), UnityEngine.Random.Range(-300, 300));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _SizeListener.Update();

        _waveProcessor.Update();
        _waveBaker.Update(_waveProcessor.GetWave(), _TimeScale, Time.time);
    }

    private void OnDestroy()
    {
        _waveProcessor.Destroy();

        _waveBaker.Dispose();
    }

    #endregion
    
    #region Initialization

    private void InitializeLoopback()
    {
        //Loopback objects
        _waveProcessor = new WaveProcessor(
            new BandData()
            {
                _minimumFrequency = 60,
                _maximumFrequency = 1150
            },
            new List<(DSPFilters, float)>()
            {
                (DSPFilters.LowPass, 1000),
                //(DSPFilters.HighPass, 500)
            },
            _SpectrumRes, 3, _SpectrumRes
        );
        
        _waveBaker = new WaveBaker(_NoiseCompute, new int2(texWidth, texHeight), new float2(_Frequency1, _Frequency2));
        _waveBaker.BindWaveData(out _NoiseTex);

    }
    
    private void GenerateLayers()
    {
        _LayerContainer = new List<Transform>();

        _nLevels = _Levels;

        float step = (1.0f) / _nLevels;
        ColorTemplate _CurrentColorScheme = _ColorSchemes[_CurrentColorIndex];

        //Create the RawImages to store every layer
        for (int i = 0; i < _nLevels; i++)
        {
            var obj = Instantiate(_TargetQuad, _UIHolder.transform);
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
    }

    #endregion

    public void GetWave(out float[] wave)
    {
        wave = _waveProcessor.GetWave();
    }
    
    public void NextColorScheme()
    {
        _CurrentColorIndex = (_CurrentColorIndex + 1) % _ColorSchemes.Length;
        SetColorScheme();
    }

    public void SetColorScheme()
    {
        float step = (1.0f) / _nLevels;

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
