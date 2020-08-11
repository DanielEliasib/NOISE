using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveDrawer
{
    public ComputeShader _Shader;
    public float[] _Wave;
    public int _Width = 10, _Height = 10;
    public RenderTexture _Tex;
    public RawImage _Target;

    private ComputeBuffer _Buffer;
    private int _KernelIndex;
    private string _KernelName = "WaveRenderer";

    public WaveDrawer(int width, int height, ComputeShader shader, RawImage target)
    {
        _Shader = shader;

        _Width = width;
        _Height = height;
        _Tex = new RenderTexture(_Width, _Height, 0, RenderTextureFormat.ARGB32);
        _Tex.filterMode = FilterMode.Point;
        _Tex.enableRandomWrite = true;
        _Tex.Create();

        _Target = target;

        _Target.texture = _Tex;

        _Shader.SetTexture(_KernelIndex, "Result", _Tex);
        _Shader.SetFloat("_Width", _Width);
        _Shader.SetFloat("_Height", _Height);
    }

    public void SetComputeData(float[] wave, float prom, float beatThreshold)
    {
        _Wave = wave;
        try
        {
            _Buffer.Dispose();
        }
        catch { }
        _Buffer = new ComputeBuffer(_Wave.Length, sizeof(float));
        _Buffer.SetData(_Wave);

        _Shader.SetBuffer(_KernelIndex, "_Line", _Buffer);
        _Shader.SetInt("_LineWidth", _Wave.Length);
        _Shader.SetFloat("_Prom", prom);
        _Shader.SetFloat("_BeatThreshold", beatThreshold);

        _Shader.Dispatch(_KernelIndex, _Width / 8, _Height / 8, 1);
    }
}
