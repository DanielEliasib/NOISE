using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AL.AudioSystem;
using Unity.Mathematics;

public class AudioAnalyser 
{
    private List<float4> _WaveData;
    private List<float4> _AFData;
    private List<float> _AmpData;

    private LoopbackCapture _Loopback;

    private List<float>[] _LongBandProms;
    private float[] _Peaks;

    List<BandData> _BandData;
    List<(DSPFilters, float)>[] _FilterData;

    private bool _CooldownActive;
    private double _Cooldown;

    public float[] _BeatMultipliers;
    public float[] _LongProms;
    public float[][] _Spectrum;

    public AudioAnalyser(List<BandData> bandData, List<(DSPFilters, float)>[] filterData, int spectrumResolution, int nBuffer)
    {
        _WaveData = new List<float4>();
        _AFData = new List<float4>();
        _AmpData = new List<float>();

        //! Loopback objects
        _BandData = bandData;
        _FilterData = filterData;

        _Loopback = new LoopbackCapture(spectrumResolution, ScalingStrategy.Sqrt, _BandData, _FilterData);

        _Loopback.StartListening();

        _LongBandProms = new List<float>[_BandData.Count];
        _BeatMultipliers = new float[_BandData.Count];
        _Peaks = new float[_BandData.Count];
        _LongProms = new float[_BandData.Count];

        for (int i = 0; i < _LongBandProms.Length; i++)
        {
            _LongBandProms[i] = new List<float>(nBuffer);
            _BeatMultipliers[i] = 1.5f;
            _Peaks[i] = float.MinValue;
        }

        _CooldownActive = false;
    }

    public void BindArrayData(out float[] beatMultiplier, out float[] longProms, out float[][] spectrum)
    {
        beatMultiplier = _BeatMultipliers;
        longProms = _LongProms;
        spectrum = _Spectrum;
    }

    public void BindWaveData(out List<float4> waveData, out List<float4> afData, out List<float> ampData)
    {
        waveData = _WaveData;
        afData = _AFData;
        ampData = _AmpData;
    }

    public void Destroy()
    {
        _Loopback.StopListening();
    }

    #region Audio Analysis
    public void ProcessAudio(float time)
    {
        _Cooldown -= time;

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
                    if (j <= 0)
                    {
                        if (_Spectrum[i][j] > _Spectrum[i][j + 1])
                            _Peaks[i] = _Spectrum[i][j];
                    }
                    else if (j >= _Spectrum.Length - 1)
                    {
                        if (_Spectrum[i][j] > _Spectrum[i][j - 1])
                            _Peaks[i] = _Spectrum[i][j];
                    }
                    else
                    {
                        if (_Spectrum[i][j] > _Spectrum[i][j - 1] && _Spectrum[i][j] > _Spectrum[i][j + 1])
                            _Peaks[i] = _Spectrum[i][j];
                    }

                }

                _BandProms[i] = _Spectrum[i].Length <= 0 ? 0 : _BandProms[i] / _Spectrum[i].Length;


            }
        }

        if (!_CooldownActive)
        {
            for (int i = 0; i < _LongBandProms.Length; i++)
            {
                var timeProm = 0.0f;

                foreach (var prom in _LongBandProms[i])
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

        for (int i = 0; i < _LongBandProms.Length; i++)
        {
            if (_LongBandProms[i].Count >= _LongBandProms[i].Capacity && _LongBandProms[i].Capacity > 0)
                _LongBandProms[i].RemoveAt(0);

            _LongBandProms[i].Add(_BandProms[i]);
        }

        if (_Cooldown <= 0)
            _CooldownActive = false;

    }
    #endregion

    #region Wave Processing
    void CreateWave(float w, float k, float amp, float decay)
    {
        _WaveData.Add(new float4(w / k, 2 * Mathf.PI / k, k, w));
        _AFData.Add(new float4(amp, decay, w / (2 * Mathf.PI), Time.time));
        _AmpData.Add(amp);
        _CooldownActive = true;
    }
    #endregion
}
