using AL.AudioSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

using System.Threading;
using System.Threading.Tasks;
using System.Linq;

public class AudioTester : MonoBehaviour
{
    [SerializeField] RenderTexture Texture;
    Texture2D _InnerTexture;
    LoopbackCapture _Loopback;

    [SerializeField] LineRenderer _Line;

    Vector3[] _LinePos;

    float[] _PosPeaks;
    int[] _PosPeaksIndex;
    float[] _PrePeaks;
    int[] _PrePeaksIndex;

    float[] _Spectrum;

    List<float> _BlueBandProms;
    bool _ProcessBlue, _SendWave;
    int _InactiveFrameCounter;

    int _SpectrumRes;

    float _BlueLongProm = 0;
    float _BlueCurrentProm = 0;

    [SerializeField, Range(1, 5)] float _BeatMultiplier = 2;


    // Start is called before the first frame update
    void Start()
    {
        _SpectrumRes = Texture.width;
        _Loopback = new LoopbackCapture(_SpectrumRes, ScalingStrategy.Sqrt);
        _Loopback.StartListening();

        _InnerTexture = new Texture2D(Texture.width, Texture.height, TextureFormat.RGBAHalf, false);

        _LinePos = new Vector3[Texture.width];

        _PrePeaks = new float[1];
        _PosPeaks = new float[1];

        _PosPeaksIndex = new int[1];
        _PrePeaksIndex = new int[1];

        _ProcessBlue = true;
        _SendWave = false;

        _BlueBandProms = new List<float>(67);
        _InactiveFrameCounter = 0;
    }

    // Update is called once per frame
    void Update()
    {

        //Gets the spectrum
        var _Spectrum = _Loopback.SpectrumData;
        this._Spectrum = _Spectrum;
        if (_Spectrum != null)
        {
            _LinePos = new Vector3[_Spectrum.Length];

            for (int i = 0; i < _LinePos.Length; i++)
                _LinePos[i] = new Vector3(i * 0.01f, _Spectrum[i], 0);

            //NewIndexes returns the index in the spectrum
            var (newArray, newIndexes) = FindPeaks(ref _Spectrum);

            //PosIndexes return the index in newIndexes
            var (posPeaks, posIndexes) = FindPeaks(ref newArray);


            _PrePeaksIndex = newIndexes.ToArray();
            _PrePeaks = newArray.ToArray();

            _PosPeaksIndex = posIndexes.ToArray();
            _PosPeaks = posPeaks.ToArray();

            //Debug.Log("Size: " + _Spectrum.Length);
            for (int i = 0; i < posPeaks.Length; i++)
            {
                for (int j = 0; j < Texture.height; j++)
                    _InnerTexture.SetPixel(i, j, new Color(i * 0.05f, posPeaks[i], 0, 1));

                //Debug.Log("Current: " + _InnerTexture.getp);
            }

            _ProcessBlue = true;
        }
        _InnerTexture.Apply();

        if (_Line != null)
        {
            _Line.positionCount = _LinePos.Length;
            _Line.SetPositions(_LinePos);
        }

        Graphics.CopyTexture(_InnerTexture, Texture);
    }

    private void FixedUpdate()
    {
        //if (_InactiveFrameCounter >= 5)
        //{
        //    _InactiveFrameCounter = 0;
        //    _ProcessBlue = true;
        //}

        //if (!_ProcessBlue)
        //{
        //    _InactiveFrameCounter++;
        //}

        if (_Spectrum != null)
            BandProcessor();
    }

    private void OnDestroy()
    {
        _Loopback.StopListening();
    }

    private (float[], int[]) FindPeaks(ref float[] inputArray)
    {
        List<float> _Peaks = new List<float>();
        List<int> _PeaksIndex = new List<int>();

        bool add = false;

        for (int i = 0; i < inputArray.Length; i++)
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
        Gizmos.color = new Color(0, 0.25f, 0.8f);
        if (_SendWave)
        {
            Gizmos.DrawSphere(new Vector3(0, 2, 0), 0.75f);
            _SendWave = false;
        }
        else
        {
            Gizmos.DrawSphere(new Vector3(0, 2, 0), 0.5f);
        }
        if (_Spectrum != null)
        {
            Gizmos.color = Color.white;

            for (int i = 0; i < _Spectrum.Length; i++)
            {
                Gizmos.color = Color.white;

                if (i / (float)_SpectrumRes >= 0 && i / (float)_SpectrumRes <= 0.8f)
                    Gizmos.color = Color.blue;

                Gizmos.DrawWireSphere(new Vector3(i * 0.01f, _Spectrum[i]), 0.01f);
            }



            for (int i = 0; i < _PosPeaks.Length; i++)
            {
                Gizmos.color = Color.red;

                if (_PrePeaksIndex[_PosPeaksIndex[i]] / (float)_SpectrumRes >= 0 && _PrePeaksIndex[_PosPeaksIndex[i]] / (float)_SpectrumRes <= 0.8f)
                    Gizmos.color = Color.blue;

                Gizmos.DrawWireSphere(new Vector3(_PrePeaksIndex[_PosPeaksIndex[i]] * 0.01f, _PosPeaks[i]), 0.02f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(0, _BlueLongProm), new Vector3(_SpectrumRes * 0.01f, _BlueLongProm));

            Gizmos.color = Color.black;
            Gizmos.DrawLine(new Vector3(0, _BlueCurrentProm), new Vector3(_SpectrumRes * 0.01f, _BlueCurrentProm));

            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(0, Mathf.Min(_BeatMultiplier * _BlueLongProm, _BlueLongProm + 1)), new Vector3(_SpectrumRes * 0.01f, Mathf.Min(_BeatMultiplier * _BlueLongProm, _BlueLongProm + 1))); ;
        }

    }

    void SendBlueWave()
    {
        _SendWave = true;
        //_ProcessBlue = false;

    }

    void BandProcessor()
    {
        //! Blue
        float4 _Proms = 0;
        int4 _NumberPoints = 0;


        for (int i = 0; i < _Spectrum.Length; i++)
        {

            var horVal = i / (float)_SpectrumRes;

            if (horVal >= 0 && horVal <= 0.8f)
            {
                _Proms.x += _Spectrum[i];
                _NumberPoints.x++;
            }
            else if (horVal > 0.8f && horVal <= 0.80f)
            {
                _Proms.y += _Spectrum[i];
                _NumberPoints.y++;
            }
            else if (horVal > 0.8f && horVal <= 0.95f )
            {
                _Proms.z += _Spectrum[i];
                _NumberPoints.z++;
            }
            else if (horVal > 0.95f)
            {
                _Proms.w += _Spectrum[i];
                _NumberPoints.w++;
            }
        }

        _Proms.x = _NumberPoints.x <= 0 ? 0 : _Proms.x / _NumberPoints.x;
        _Proms.y /= _NumberPoints.y;
        _Proms.z /= _NumberPoints.z;
        _Proms.w /= _NumberPoints.w;

        _BlueCurrentProm = _Proms.x;

        var send = true;

        //**********************************
        if (_ProcessBlue)
        {
            var longProm = 0.0f;
            Debug.Log("Number of proms: " + _BlueBandProms.Count);
            foreach (var prom in _BlueBandProms)
            {
                longProm += prom;
            }

            longProm = _BlueBandProms.Count <= 0 ? 0 : longProm / _BlueBandProms.Count;

            _BlueLongProm = longProm;

            if (_Proms.x >= Mathf.Min(_BeatMultiplier * _BlueLongProm, _BlueLongProm + 1))
            {
                //send = false;
                SendBlueWave();
            }

        }

        //**********************************
        if (send)
        {
            if (_BlueBandProms.Count >= _BlueBandProms.Capacity && _BlueBandProms.Capacity > 0)
                _BlueBandProms.RemoveAt(0);

            _BlueBandProms.Add(_Proms.x);
        }

        //if (_ProcessBlue)
        //{
        //    var longProm = 0.0f;

        //    foreach(var prom in _BlueBandProms)
        //    {
        //        longProm += prom;
        //    }

        //    longProm = _BlueBandProms.Count <= 0 ? 0 : longProm / _BlueBandProms.Count;

        //    //_BlueLongProm = longProm;

        //    var med = 0.0f;

        //    foreach(var prom in _BlueBandProms)
        //    {
        //        med += (longProm - prom) * (longProm - prom);
        //    }

        //    med = _BlueBandProms.Count <= 0 ? 0 : Mathf.Sqrt(med / _BlueBandProms.Count);
        //    _BlueLongProm = med;
        //    if (_Proms.x >= _BeatMultiplier * med)
        //    {
        //        send = false;
        //        SendBlueWave();
        //    }
        //}

        //! Blue band time-process
        //if (_BluePeaks.Count >= _BluePeaks.Capacity && _BluePeaks.Capacity > 0)
        //    _BluePeaks.RemoveAt(0);

        //_BluePeaks.Add(_Proms.x);

        //if (_ProcessBlue)
        //{
        //    var blueProm = 0.0f;

        //    for (int i = 0; i < _BluePeaks.Count; i++)
        //    {
        //        blueProm += _BluePeaks[i];
        //    }

        //    blueProm = _BluePeaks.Count <= 0 ? 0 : blueProm / _BluePeaks.Count;

        //    var med = 0.0f;

        //    for (int i = 0; i < _BluePeaks.Count; i++)
        //    {
        //        med += (blueProm - _BluePeaks[i]) * (blueProm - _BluePeaks[i]);
        //    }

        //    med = _BluePeaks.Count <= 0 ? 0 : Mathf.Sqrt(med / _BluePeaks.Count);

        //    if (med >= 0.25f)
        //        SendBlueWave();
        //}

    }
}
