using AL.AudioSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Unity.Mathematics;

public class AudioTester : MonoBehaviour
{
    [SerializeField] RenderTexture Texture;
    Texture2D _InnerTexture;
    LoopbackCapture _Loopback;

    [SerializeField] LineRenderer _Line;

    Vector3[] _Pos;
    // Start is called before the first frame update
    void Start()
    {
        _Loopback = new LoopbackCapture(Texture.width, ScalingStrategy.Sqrt);
        _Loopback.StartListening();

        _InnerTexture = new Texture2D(Texture.width, Texture.height, TextureFormat.RGBAHalf, false);

        _Pos = new Vector3[Texture.width];
    }

    // Update is called once per frame
    void Update()
    {
        

        var _Spectrum = _Loopback.SpectrumData;
        if (_Spectrum != null)
        {

            Debug.Log("Size: " + _Spectrum.Length);
            for (int i = 0; i < _Spectrum.Length; i++)
            {
                float val = _Spectrum[i] > 2.5f? _Spectrum[i] : 0.0f;
                _Pos[i] = new Vector3(i*0.01f, val, 0);
                for(int j = 0; j < Texture.height; j++)
                    _InnerTexture.SetPixel(i, j, new Color(i*0.01f, val, 0, 1));

                //Debug.Log("Current: " + _InnerTexture.getp);
            }
                
        }
        _InnerTexture.Apply();

        if(_Line != null)
        {
            _Line.positionCount = _Pos.Length;
            _Line.SetPositions(_Pos);
        }

        Graphics.CopyTexture(_InnerTexture, Texture);
    }

    private void OnDestroy()
    {
        //_Loopback.StopListening();
    }
}
