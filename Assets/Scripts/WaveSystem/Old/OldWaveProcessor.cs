using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;

public class OldWaveProcessor
{
    private List<float4> _WaveData;
    private List<float4> _WaveDataBack;

    private List<float4> _AFData;
    private List<float4> _AFDataBack;

    private List<float> _AmpData;
    private List<float> _AmpDataBack;

    private RenderTexture _NoiseTexture;

    //! ----
    ComputeBuffer _WaveDataBuffer;
    ComputeBuffer _AFDataBuffer;
    ComputeBuffer _AmpDataBuffer;

    private ComputeShader _NoiseCompute;

    private int _NoiseKernelIndex;
    private string _NoiseKernelName = "NoiseLayerGenerator";

    private int texWidth, texHeight;

    private float _TextureRadius = 0;
    private float _Frecuency1, _Frecuency2;
    private float2 _Offset1, _Offset2;

    public OldWaveProcessor(ComputeShader shader, int2 size, float2 frec)
    {
        _NoiseCompute = shader;

        texWidth = size.x;
        texHeight = size.y;

        _TextureRadius = Mathf.Sqrt(texWidth * texWidth * 0.25f + texHeight * texHeight * 0.25f);

        _NoiseTexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32);
        _NoiseTexture.enableRandomWrite = true;
        _NoiseTexture.Create();

        InitCompute();

        _WaveDataBack = new List<float4>();
        _AFDataBack = new List<float4>();
        _AmpDataBack = new List<float>();

        _Offset1 += new float2(UnityEngine.Random.Range(-300, 300), UnityEngine.Random.Range(-300, 300));
        _Offset2 += new float2(UnityEngine.Random.Range(-300, 300), UnityEngine.Random.Range(-300, 300));

        _Frecuency1 = frec.x;
        _Frecuency2 = frec.y;

    }

    public void Update(float2 timeScale, float time)
    {
        _Offset1.y = time * timeScale.x;
        _Offset2.x = time * timeScale.y;

        ProcessWaveData();
        WaveBufferManager(_WaveData.Count);

        DispatchNoiseShaders();
    }

    public void BindWaveData(in List<float4> waveData, in List<float4> afData, in List<float> ampData, out RenderTexture noiseTexture)
    {
        _WaveData = waveData;
        _AFData = afData;
        _AmpData = ampData;
        noiseTexture = _NoiseTexture;
    }

    public void Dispose()
    {
        _AFDataBuffer.Dispose();
        _WaveDataBuffer.Dispose();
        _AmpDataBuffer.Dispose();
    }

    #region ComputeFunctions
    void InitCompute()
    {
        _NoiseKernelIndex = _NoiseCompute.FindKernel(_NoiseKernelName);

        //_LayerCompute.SetTexture(_LayerKernelIndex, "Layers", _Layers);

        _NoiseCompute.SetInt("_TexWidth", texWidth);
        _NoiseCompute.SetInt("_TexHeight", texHeight);

        _WaveDataBuffer = new ComputeBuffer(25, sizeof(float) * 4);
        _AFDataBuffer = new ComputeBuffer(25, sizeof(float) * 4);
        _AmpDataBuffer = new ComputeBuffer(25, sizeof(float));

        //_LayerCompute.SetBuffer(_LayerKernelIndex, "_NoiseLayer", _NoiseDataBuffer);

        //! Noise
        _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_WaveData", _WaveDataBuffer);
        _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_AFData", _AFDataBuffer);
        _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_Amplituds", _AmpDataBuffer);
        _NoiseCompute.SetTexture(_NoiseKernelIndex, "_NoiseData", _NoiseTexture);
        _NoiseCompute.SetInt("_TexWidth", texWidth);
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
        _NoiseCompute.Dispatch(_NoiseKernelIndex, texWidth / 8, texHeight / 8, 1);
    }
    #endregion

    #region Wave Processing
    void ProcessWaveData()
    {
        _AFDataBack.Clear();
        _WaveDataBack.Clear();
        _AmpDataBack.Clear();

        for (int i = 0; i < _WaveData.Count; i++)
        {
            //Check if the wave already leaved the screen, if so it removes it from the data
            if (!(_WaveData[i].x * (Time.time - _AFData[i].w) - _WaveData[i].y * 0.5f > _TextureRadius))
            {
                _WaveDataBack.Add(_WaveData[i]);
                _AFDataBack.Add(_AFData[i]);
                // Pre calculate amplituds
                _AmpDataBack.Add(_AFData[i].x / Mathf.Exp(_AFData[i].y * (Time.time - _AFData[i].w)));
            }
        }

        _WaveData.Clear();
        _WaveData.AddRange(_WaveDataBack);

        _AFData.Clear();
        _AFData.AddRange(_AFDataBack);

        _AmpData.Clear();
        _AmpData.AddRange(_AmpDataBack);

        //_WaveData = new List<float4>(_WaveDataBack);
        //_AFData = new List<float4>(_AFDataBack);
        //_AmpData = new List<float>(_AmpDataBack);
    }
    #endregion
}
