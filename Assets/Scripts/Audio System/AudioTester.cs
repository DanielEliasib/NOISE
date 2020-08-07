using AL.AudioSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Unity.Mathematics;
using UnityEngine.Rendering;

public class AudioTester : MonoBehaviour
{
    [SerializeField] RenderTexture Texture;
    Texture2D _InnerTexture;
    LoopbackCapture _Loopback;

    [SerializeField] LineRenderer _Line;

    Vector3[] _LinePos;
    Vector3[] _PosPeaks;
    Vector3[] _PrePeaks;

    // Start is called before the first frame update
    void Start()
    {
        _Loopback = new LoopbackCapture(Texture.width, ScalingStrategy.Sqrt);
        _Loopback.StartListening();

        _InnerTexture = new Texture2D(Texture.width, Texture.height, TextureFormat.RGBAHalf, false);

        _LinePos = new Vector3[Texture.width];

    }

    // Update is called once per frame
    void Update()
    {
        
        //Gets the spectrum
        var _Spectrum = _Loopback.SpectrumData;
        if (_Spectrum != null)
        {
            var reindexedSpectrum = _Spectrum;

            _LinePos = new Vector3[reindexedSpectrum.Length];

            for (int i = 0; i < _LinePos.Length; i++)
                _LinePos[i] = new Vector3(i * 0.05f, reindexedSpectrum[i], 0);

            var (newArray, newIndexes) = FindPeaks(ref reindexedSpectrum);
            
            var (posPeaks, posIndexes) = FindPeaks(ref newArray);

            _PrePeaks = new Vector3[newArray.Length]; 

            for(int i = 0; i < newArray.Length; i++)
            {
                _PrePeaks[i] = new Vector3(newIndexes[i] * 0.05f, newArray[i], 0);
            }

            _PosPeaks = new Vector3[posPeaks.Length];

            //Debug.Log("Size: " + _Spectrum.Length);
            for (int i = 0; i < posPeaks.Length; i++)
            {
                _PosPeaks[i] = new Vector3(newIndexes[posIndexes[i]] * 0.05f, posPeaks[i], 0);

                for(int j = 0; j < Texture.height; j++)
                    _InnerTexture.SetPixel(i, j, new Color(i*0.05f, posPeaks[i], 0, 1));

                //Debug.Log("Current: " + _InnerTexture.getp);
            }
                
        }
        _InnerTexture.Apply();

        if (_Line != null)
        {
            _Line.positionCount = _LinePos.Length;
            _Line.SetPositions(_LinePos);
        }

        Graphics.CopyTexture(_InnerTexture, Texture);
    }

    private void OnDestroy()
    {
        //_Loopback.StopListening();
    }

    private float[] Reindexer(ref float[] inputArray)
    {
        double a = Mathf.Pow(inputArray.Length, 1.0f / inputArray.Length);
        List<float> _ReindexedArray = new List<float>();

        int oldIndex = -1;

        for(int i = 0; i < inputArray.Length; i++)
        {
            int lookUpIndex = (int)System.Math.Log(i, a);
            if (oldIndex != lookUpIndex && lookUpIndex<inputArray.Length && lookUpIndex>=0)
                _ReindexedArray.Add(inputArray[lookUpIndex]);
        }

        return _ReindexedArray.ToArray();
    }

    private (float[], int[]) FindPeaks(ref float[] inputArray)
    {
        List<float> _Peaks = new List<float>();
        List<int> _PeaksIndex = new List<int>();

        bool add = false;

        for(int i = 0; i < inputArray.Length; i++)
        {
            try
            {
                add = (i == 0 && inputArray[i] > inputArray[i + 1]) ||
                (i == inputArray.Length - 1 && inputArray[i] >= inputArray[i - 1]) ||
                (inputArray[i] > inputArray[i + 1] && inputArray[i] >= inputArray[i - 1]);
            }
            catch { add = false; }
            

            if (add)
            {
                _Peaks.Add(inputArray[i]);
                _PeaksIndex.Add(i);
                //Debug.Log("Point: " + inputArray[i]);
            }
        }
        return (_Peaks.ToArray(), _PeaksIndex.ToArray());
    }

    void OnDrawGizmos()
    {
        float scale = 0.05f;

        float4 _Proms = 0;
        int4 _NumberPoints = 0;

        if (_PosPeaks != null)
        {
            
            //Debug.Log("Post lenght: " + _PosPeaks.Length);
            for(int i = 0; i < _PosPeaks.Length; i++)
            {
                Gizmos.color = Color.red;

                float3 currentPoint = _PosPeaks[i];
                if(currentPoint.y > 0.0f)
                {
                    if (currentPoint.x >= 0 && currentPoint.x <= 50 * scale)
                    {
                        Gizmos.color = Color.blue;

                        _Proms.x += currentPoint.y;
                        _NumberPoints.x += 1;
                    }
                    else if (currentPoint.x > 50 * scale && currentPoint.x <= 80 * scale)
                    {
                        Gizmos.color = Color.cyan;

                        _Proms.y += currentPoint.y;
                        _NumberPoints.y += 1;
                    }
                    else if (currentPoint.x > 80 * scale && currentPoint.x <= 95 * scale)
                    {
                        Gizmos.color = Color.green;

                        _Proms.z += currentPoint.y;
                        _NumberPoints.z += 1;
                    }
                    else if (currentPoint.x > 95 * scale)
                    {
                        Gizmos.color = Color.magenta;

                        _Proms.w += currentPoint.y;
                        _NumberPoints.w += 1;
                    }
                    //Gizmos.DrawWireSphere(_PosPeaks[i], 0.05f);
                }
            }

            _Proms.x /= _NumberPoints.x;
            _Proms.y /= _NumberPoints.y;
            _Proms.z /= _NumberPoints.z;
            _Proms.w /= _NumberPoints.w;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(new Vector3(0, _Proms.x, 0), 0.05f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(0, _Proms.y, 0), 0.05f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(0, _Proms.z, 0), 0.05f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(new Vector3(0, _Proms.w, 0), 0.05f);

        }

        Gizmos.color = Color.white;

        if (_PrePeaks != null)
        {
            for (int i = 0; i < _PrePeaks.Length; i++)
            {
                

                Gizmos.DrawWireSphere(_PrePeaks[i], 0.01f);
            }
        }
    }

    List<float> UnbiasPicker(List<float> pointsX)
    {
        List<float> peaks = new List<float>();

        bool add;
        for(int i = 0; i < pointsX.Count; i++)
        {
            try
            {
                add = (i == 0 && pointsX[i] > pointsX[i + 1]) ||
                    (i == pointsX.Count - 1 && pointsX[i] >= pointsX[i - 1]) ||
                    (pointsX[i] > pointsX[i + 1] && pointsX[i] >= pointsX[i - 1]);
            }
            catch { add = false; }

            if (add)
                peaks.Add(pointsX[i]);
        }

        return peaks;

    }
}
