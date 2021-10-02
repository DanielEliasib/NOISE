using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class WaveBaker
{
   
    private readonly RenderTexture _NoiseTexture;
    private readonly ComputeShader _NoiseCompute;
    
    private const string _NoiseKernelName = "NoiseLayerGenerator";
    
    private ComputeBuffer _WaveDataBuffer;
    
    private int _NoiseKernelIndex;

    private readonly int texWidth, texHeight;

    private readonly float _TextureRadius = 0;
    private readonly float _Frequency1, _Frequency2;
    private float2 _Offset1, _Offset2;

    public WaveBaker(ComputeShader shader, int2 size, float2 freq)
    {
        _NoiseCompute = shader;

        texWidth = size.x;
        texHeight = size.y;

        _TextureRadius = Mathf.Sqrt(texWidth * texWidth * 0.25f + texHeight * texHeight * 0.25f);

        _NoiseTexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGB32);
        _NoiseTexture.enableRandomWrite = true;
        _NoiseTexture.Create();
        
        InitCompute();
        
        _Offset1 += new float2(UnityEngine.Random.Range(-300, 300), UnityEngine.Random.Range(-300, 300));
        _Offset2 += new float2(UnityEngine.Random.Range(-300, 300), UnityEngine.Random.Range(-300, 300));

        _Frequency1 = freq.x;
        _Frequency2 = freq.y;
    }
    
    public void Update(in float[] waveData, float2 timeScale, float time)
    {
        _Offset1.y = time * timeScale.x;
        _Offset2.x = time * timeScale.y;

        if(waveData is null)
            return; 
        
        SetWaveData(waveData);

        DispatchNoiseShaders();
    }
    
    public void BindWaveData(out RenderTexture noiseTexture)
    {
        noiseTexture = _NoiseTexture;
    }

    private void InitCompute()
    {
        _NoiseKernelIndex = _NoiseCompute.FindKernel(_NoiseKernelName);
        
        //! Noise
        _NoiseCompute.SetTexture(_NoiseKernelIndex, "_NoiseData", _NoiseTexture);
        
        _NoiseCompute.SetInt("_TexWidth", texWidth);
        _NoiseCompute.SetInt("_TexHeight", texHeight);
        
        _NoiseCompute.SetFloat("rad", _TextureRadius);
        
    }

    private void SetNoiseParameters(float frequency1, float frequency2, float2 offset1, float2 offset2)
    {
        _NoiseCompute.SetFloat("_Time", Time.time);

        _NoiseCompute.SetFloat("_Freq1", frequency1);
        _NoiseCompute.SetFloat("_Freq2", frequency2);

        _NoiseCompute.SetFloats("_Offset1", new float[] { offset1.x, offset1.y });
        _NoiseCompute.SetFloats("_Offset2", new float[] { offset2.x, offset2.y });
    }

    private void DispatchNoiseShaders()
    {
        SetNoiseParameters(_Frequency1, _Frequency2, _Offset1, _Offset2);
        _NoiseCompute.Dispatch(_NoiseKernelIndex, texWidth / 8, texHeight / 8, 1);
    }

    private void SetWaveData(in float[] waveData)
    {
        _WaveDataBuffer?.Dispose();
        _WaveDataBuffer = new ComputeBuffer(waveData.Length, sizeof(float));
        _NoiseCompute.SetBuffer(_NoiseKernelIndex, "_WaveData", _WaveDataBuffer);
        
        _WaveDataBuffer.SetData(waveData);
        
        _NoiseCompute.SetInt("dataSize", waveData.Length-1);
    }
    
    public void Dispose()
    {
        _WaveDataBuffer.Dispose();
    }
}
