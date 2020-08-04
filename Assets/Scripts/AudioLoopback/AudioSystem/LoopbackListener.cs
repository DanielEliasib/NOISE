using Assets.Scripts.Audio;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

//Doesn't need to be a mono
public class LoopbackListener
{
    #region Constants

    private const int EnergyAverageCount = 100;

    #endregion

    #region Private Member Variables

    private RealtimeAudio _RealtimeAudio;
    private List<float> _PostScaleAverages = new List<float>();

    #endregion

    #region Public Properties

    public int SpectrumSize;
    public ScalingStrategy ScalingStrategy;
    public float[] SpectrumData;
    public float[] PostScaledSpectrumData;
    public float[] PostScaledMinMaxSpectrumData;
    public float PostScaledMax;
    public float PostScaledEnergy;
    public bool IsIdle;

    // Set through editor, but good values are 0.8, 0.5, 1.2, 1.5 respectively
    public float ThresholdToMin;
    public float MinAmount;
    public float ThresholdToMax;
    public float MaxAmount;

    #endregion

    #region Startup / Shutdown

    public LoopbackListener(int spectrumSize, ScalingStrategy scalingStrategy, float threshholdMin, float minAmount, float thresholMax, float maxAmount)
    {
        SpectrumSize = spectrumSize;
        ScalingStrategy = scalingStrategy;
        ThresholdToMin = threshholdMin;
        MinAmount = minAmount;
        ThresholdToMax = threshholdMin;
        MaxAmount = maxAmount;

        SpectrumData = new float[SpectrumSize];
        PostScaledSpectrumData = new float[SpectrumSize];
        PostScaledMinMaxSpectrumData = new float[SpectrumSize];

        // Used for post scaling
        float postScaleStep = 1.0f / SpectrumSize;

        // Setup loopback audio and start listening
        _RealtimeAudio = new RealtimeAudio(SpectrumSize, ScalingStrategy, (spectrumData) =>
        {
            // Raw
            SpectrumData = spectrumData;

            // Post scaled for visualization
            float postScaledPoint = postScaleStep;
            float postScaledMax = 0.0f;

            float postScaleAverage = 0.0f;
            float totalPostScaledValue = 0.0f;

            bool isIdle = true;

            // Pass 1: Scaled. Scales progressively as moving up the spectrum
            for (int i = 0; i < SpectrumSize; i++)
            {
                // Don't scale low band, it's too useful
                if (i == 0)
                {
                    PostScaledSpectrumData[i] = SpectrumData[i];
                }
                else
                {
                    float postScaleValue = postScaledPoint * SpectrumData[i] * (RealtimeAudio.MaxAudioValue - (1.0f - postScaledPoint));
                    PostScaledSpectrumData[i] = Mathf.Clamp(postScaleValue, 0, RealtimeAudio.MaxAudioValue); // TODO: Can this be done better than a clamp?
                }

                if (PostScaledSpectrumData[i] > postScaledMax)
                {
                    postScaledMax = PostScaledSpectrumData[i];
                }

                postScaledPoint += postScaleStep;
                totalPostScaledValue += PostScaledSpectrumData[i];

                if (spectrumData[i] > 0)
                {
                    isIdle = false;
                }
            }

            PostScaledMax = postScaledMax;

            // Calculate "energy" using the post scale average
            postScaleAverage = totalPostScaledValue / SpectrumSize;
            _PostScaleAverages.Add(postScaleAverage);

            // We only want to track EnergyAverageCount averages.
            // With a value of 1000, this will happen every couple seconds
            if (_PostScaleAverages.Count == EnergyAverageCount)
            {
                _PostScaleAverages.RemoveAt(0);
            }

            // Average the averages to get the energy.
            PostScaledEnergy = _PostScaleAverages.Average();

            // Pass 2: MinMax spectrum. Here we use the average.
            // If a given band falls below the average, reduce it 50%
            // otherwise boost it 50%
            for (int i = 0; i < SpectrumSize; i++)
            {
                float minMaxed = PostScaledSpectrumData[i];

                if (minMaxed <= postScaleAverage * ThresholdToMin)
                {
                    minMaxed *= MinAmount;
                }
                else if (minMaxed >= postScaleAverage * ThresholdToMax)
                {
                    minMaxed *= MaxAmount;
                }

                PostScaledMinMaxSpectrumData[i] = minMaxed;
            }

            IsIdle = isIdle;
        });
        _RealtimeAudio.StartListen();
    }

    public void OnDisable()
    {
        _RealtimeAudio.StopListen();
    }

    #endregion

    #region Public Methods

    public float[] GetAllSpectrumData(AudioVisualizationStrategy strategy)
    {
        float[] spectrumData;

        switch (strategy)
        {
            case AudioVisualizationStrategy.Raw:
                spectrumData = SpectrumData;
                break;
            case AudioVisualizationStrategy.PostScaled:
                spectrumData = PostScaledSpectrumData;
                break;
            case AudioVisualizationStrategy.PostScaledMinMax:
                spectrumData = PostScaledMinMaxSpectrumData;
                break;
            default:
                throw new InvalidOperationException(string.Format("Invalid for GetAllSpectrumData: {0}", strategy));
        }

        return spectrumData;
    }

    public float GetSpectrumData(AudioVisualizationStrategy strategy, int index = 0)
    {
        float spectrumData = 0.0f;

        switch (strategy)
        {
            case AudioVisualizationStrategy.Raw:
                spectrumData = SpectrumData[index];
                break;
            case AudioVisualizationStrategy.PostScaled:
                spectrumData = PostScaledSpectrumData[index];
                break;
            case AudioVisualizationStrategy.PostScaledMinMax:
                spectrumData = PostScaledMinMaxSpectrumData[index];
                break;
            case AudioVisualizationStrategy.PostScaledMax:
                spectrumData = PostScaledMax;
                break;
            case AudioVisualizationStrategy.PostScaledEnergy:
                spectrumData = PostScaledEnergy;
                break;
        }

        return spectrumData;
    }

    #endregion
}
