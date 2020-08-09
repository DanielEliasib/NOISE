using CSCore.DSP;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AL.AudioSystem
{
    internal class BandSpectrumProcessor : SpectrumBase
    {
        float[] fftBuffer;
        Dictionary<int, List<BiQuad>> _BandFrecuencies;

        public int BarCount
        {
            get { return SpectrumResolution; }
            set { SpectrumResolution = value; }
        }

        public BandSpectrumProcessor(FftSize fftSize, List<BandData> bandData)
        {
            FftSize = fftSize;
            SetBandData(bandData);
            _BandFrecuencies = new Dictionary<int, List<BiQuad>>(bandData.Count);
        }

        public void AddFilter(BiQuad filter, int index)
        {
            if (_BandFrecuencies.ContainsKey(index))
            {
                _BandFrecuencies.TryGetValue(index, out var filterList);
                filterList.Add(filter);
                _BandFrecuencies.Remove(index);
                _BandFrecuencies.Add(index, filterList);
            }
            else
            {
                var filterList = new List<BiQuad>();
                filterList.Add(filter);
                _BandFrecuencies.Add(index, filterList);
            }
        }

        public float[] GetSpectrumData(double maxValue, int bandIndex)
        {
            // Get spectrum data internal
            if(fftBuffer == null)
                fftBuffer = new float[(int)FftSize];

            UpdateFrequencyMapping(bandIndex);

            if (SpectrumProvider.GetFftData(fftBuffer, this))
            {
                SpectrumPointData[] spectrumPoints = CalculateSpectrumPoints(maxValue, fftBuffer, bandIndex);
                
                // Convert to float[]
                List<float> spectrumData = new List<float>();
                spectrumPoints.ToList().
                    ForEach
                    (
                        point =>
                        {
                            float val = (float)point.Value;
                            if (_BandFrecuencies.TryGetValue(bandIndex, out var filterList))
                            {
                                foreach (var filter in filterList)
                                    val = filter.Process(val);
                            }
                           
                            spectrumData.Add(val);
                        }
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