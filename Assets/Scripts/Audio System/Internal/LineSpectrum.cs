using CSCore.DSP;
using System.Collections.Generic;
using System.Linq;

namespace AL.AudioSystem
{
    internal class LineSpectrum : SpectrumBase
    {
        LowpassFilter _LowPassFilter;
        PeakFilter _PeakPassFilter;
        HighpassFilter _HighPassFilter;
        BandpassFilter _BandPassFilter;

        public int BarCount
        {
            get { return SpectrumResolution; }
            set { SpectrumResolution = value; }
        }

        public LineSpectrum(FftSize fftSize, int sampleRate, double frequency)
        {
            FftSize = fftSize;
            _LowPassFilter = new LowpassFilter(sampleRate, frequency);
            _HighPassFilter = new HighpassFilter(sampleRate, 650);
            _PeakPassFilter = new PeakFilter(sampleRate, frequency, 100, 20);
            _BandPassFilter = new BandpassFilter(sampleRate, frequency);
            
        }

        public float[] GetSpectrumData(double maxValue)
        {
            // Get spectrum data internal
            var fftBuffer = new float[(int)FftSize];

            UpdateFrequencyMapping();

            if (SpectrumProvider.GetFftData(fftBuffer, this))
            {
                SpectrumPointData[] spectrumPoints = CalculateSpectrumPoints(maxValue, fftBuffer);
                
                // Convert to float[]
                List<float> spectrumData = new List<float>();
                spectrumPoints.ToList().
                    ForEach
                    (
                        point => spectrumData.Add(_LowPassFilter.Process((float)point.Value))
                    );
                return spectrumData.ToArray();
            }

            return null;
        }

        void Test()
        {
            
        }
    }
}