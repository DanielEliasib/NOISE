using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

using AL.AudioSystem;
using Unity.Mathematics;
using Debug = System.Diagnostics.Debug;

public class AudioAnalyser 
{
    private List<float4> waveData;
    private List<float4> afData;
    private List<float> ampData;

    private LoopbackCapture loopback;

    private List<float>[] longBandProms;
    private float[] peaks;

    List<BandData> bandData;
    List<(DSPFilters, float)>[] filterData;

    private bool cooldownActive;
    private double cooldown;

    public float[] beatMultipliers;
    public float[] longProms;
    public float[][] spectrum;

    public AudioAnalyser(List<BandData> bandData, List<(DSPFilters, float)>[] filterData, int spectrumResolution, int nBuffer)
    {
        waveData = new List<float4>();
        afData = new List<float4>();
        ampData = new List<float>();

        //! Loopback objects
        this.bandData = bandData;
        this.filterData = filterData;

        loopback = new LoopbackCapture(spectrumResolution, ScalingStrategy.Sqrt, this.bandData, this.filterData);

        loopback.StartListening();

        longBandProms = new List<float>[this.bandData.Count];
        beatMultipliers = new float[this.bandData.Count];
        peaks = new float[this.bandData.Count];
        longProms = new float[this.bandData.Count];

        for (int i = 0; i < longBandProms.Length; i++)
        {
            longBandProms[i] = new List<float>(nBuffer);
            beatMultipliers[i] = 1.5f;
            peaks[i] = float.MinValue;
        }

        cooldownActive = false;
    }

    public void BindArrayData(out float[] beatMultiplier, out float[] longProms, out float[][] spectrum)
    {
        beatMultiplier = beatMultipliers;
        longProms = this.longProms;
        spectrum = this.spectrum;
    }

    public void BindWaveData(out List<float4> waveData, out List<float4> afData, out List<float> ampData)
    {
        waveData = this.waveData;
        afData = this.afData;
        ampData = this.ampData;
    }

    public void Destroy()
    {
        loopback.StopListening();
    }

    #region Audio Analysis
    public void ProcessAudio(float time)
    {
        cooldown -= time;

        spectrum = loopback.SpectrumData;

        float[] _BandProms = new float[spectrum.Length];

        for (int i = 0; i < spectrum.Length; i++)
        {
            peaks[i] = float.MinValue;

            if (spectrum[i] != null)
            {
                for (int j = 0; j < spectrum[i].Length; j++)
                {
                    _BandProms[i] += spectrum[i][j];

                    //Find Peaks
                    if (j <= 0)
                    {
                        if (spectrum[i][j] > spectrum[i][j + 1])
                            peaks[i] = spectrum[i][j];
                    }
                    else if (j >= spectrum.Length - 1)
                    {
                        if (spectrum[i][j] > spectrum[i][j - 1])
                            peaks[i] = spectrum[i][j];
                    }
                    else
                    {
                        if (spectrum[i][j] > spectrum[i][j - 1] && spectrum[i][j] > spectrum[i][j + 1])
                            peaks[i] = spectrum[i][j];
                    }

                }

                _BandProms[i] = spectrum[i].Length <= 0 ? 0 : _BandProms[i] / spectrum[i].Length;


            }
        }

        if (!cooldownActive)
        {
            for (int i = 0; i < longBandProms.Length; i++)
            {
                var timeProm = 0.0f;

                foreach (var prom in longBandProms[i])
                {
                    timeProm += prom;
                }

                timeProm = longBandProms[i].Count <= 0 ? 0 : timeProm / longBandProms[i].Count;

                longProms[i] = timeProm;

                if (peaks[i] >= Mathf.Min(beatMultipliers[i] * timeProm, timeProm + 1) && _BandProms[i] >= 0.25f)
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
                    cooldown = 0.015 * 5;
                }
            }
        }

        for (int i = 0; i < longBandProms.Length; i++)
        {
            if (longBandProms[i].Count >= longBandProms[i].Capacity && longBandProms[i].Capacity > 0)
                longBandProms[i].RemoveAt(0);

            longBandProms[i].Add(_BandProms[i]);
        }

        if (cooldown <= 0)
            cooldownActive = false;
    }
    #endregion

    #region Wave Processing
    void CreateWave(float w, float k, float amp, float decay)
    {
        waveData.Add(new float4(w / k, 2 * Mathf.PI / k, k, w));
        afData.Add(new float4(amp, decay, w / (2 * Mathf.PI), Time.time));
        ampData.Add(amp);
        cooldownActive = true;
    }
    #endregion
}
