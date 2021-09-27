using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class NewWaveProcessor
{
   
    private RenderTexture _NoiseTexture;
    
    ComputeBuffer _WaveDataBuffer;
    
    private ComputeShader _NoiseCompute;
    
    private int _NoiseKernelIndex;
    private string _NoiseKernelName = "NoiseLayerGenerator";

    private int texWidth, texHeight;

    private float _TextureRadius = 0;
    private float _Frecuency1, _Frecuency2;
    private float2 _Offset1, _Offset2;

    public NewWaveProcessor(ComputeShader shader, int2 size, float2 frec)
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

        _Frecuency1 = frec.x;
        _Frecuency2 = frec.y;
    }
    
    public void Update(in float[] waveData, float2 timeScale, float time)
    {
        _Offset1.y = time * timeScale.x;
        _Offset2.x = time * timeScale.y;

        if(waveData is null)
            return; 
        
        ProcessWaveData(waveData);

        DispatchNoiseShaders();
    }
    
    public void BindWaveData(out RenderTexture noiseTexture)
    {
        noiseTexture = _NoiseTexture;
    }
    
    void InitCompute()
    {
        _NoiseKernelIndex = _NoiseCompute.FindKernel(_NoiseKernelName);
        
        //! Noise
        _NoiseCompute.SetTexture(_NoiseKernelIndex, "_NoiseData", _NoiseTexture);
        
        _NoiseCompute.SetInt("_TexWidth", texWidth);
        _NoiseCompute.SetInt("_TexHeight", texHeight);
        
        _NoiseCompute.SetFloat("rad", _TextureRadius);
        
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

    private void ProcessWaveData(in float[] waveData)
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
