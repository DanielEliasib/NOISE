using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using CSCore;
using CSCore.DSP;

namespace AL.AudioSystem
{
    struct BandData
    {
        public int _maximumFrequency;
        public int _maximumFrequencyIndex;
        public int _minimumFrequency; //Default spectrum from 20Hz to 20kHz
        public int _minimumFrequencyIndex;
    }

    internal class SpectrumBase : INotifyPropertyChanged
    {
        private const int ScaleFactorLinear = 9;
        protected const int ScaleFactorSqr = 2;
        protected const double MinDbValue = -90;
        protected const double MaxDbValue = 0;
        protected const double DbScale = (MaxDbValue - MinDbValue);

        private int _fftSize;
        private bool _isXLogScale;
        private int _maxFftIndex;

        //private int _maximumFrequency = 150;
        //private int _maximumFrequencyIndex;
        //private int _minimumFrequency = 60; //Default spectrum from 20Hz to 20kHz
        //private int _minimumFrequencyIndex;

        //**************
        private List<BandData> _BandFrecuencies = new List<BandData>();
        //**************

        private ScalingStrategy _scalingStrategy;

        private int[] _spectrumIndexMax;
        private int[] _spectrumLogScaleIndexMax;
        private ISpectrumProvider _spectrumProvider;

        protected int SpectrumResolution;
        private bool _useAverage;

        [BrowsableAttribute(false)]
        public ISpectrumProvider SpectrumProvider
        {
            get { return _spectrumProvider; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _spectrumProvider = value;

                RaisePropertyChanged("SpectrumProvider");
            }
        }

        public void SetBandData(List<BandData> bandData)
        {
            _BandFrecuencies = bandData;
        }

        public bool IsXLogScale
        {
            get { return _isXLogScale; }
            set
            {
                _isXLogScale = value;
                for(int i = 0; i<_BandFrecuencies.Count; i++)
                    UpdateFrequencyMapping(i);
                RaisePropertyChanged("IsXLogScale");
            }
        }

        public ScalingStrategy ScalingStrategy
        {
            get { return _scalingStrategy; }
            set
            {
                _scalingStrategy = value;
                RaisePropertyChanged("ScalingStrategy");
            }
        }

        public bool UseAverage
        {
            get { return _useAverage; }
            set
            {
                _useAverage = value;
                RaisePropertyChanged("UseAverage");
            }
        }

        [BrowsableAttribute(false)]
        public FftSize FftSize
        {
            get { return (FftSize) _fftSize; }
            protected set
            {
                if ((int) Math.Log((int) value, 2) % 1 != 0)
                    throw new ArgumentOutOfRangeException("value");

                _fftSize = (int) value;
                _maxFftIndex = _fftSize / 2 - 1;

                RaisePropertyChanged("FFTSize");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void UpdateFrequencyMapping(int bandIndex)
        {
            BandData _CurrentBand = _BandFrecuencies[bandIndex];

            _CurrentBand._maximumFrequencyIndex = 
                Math.Min(_spectrumProvider.GetFftBandIndex(_CurrentBand._maximumFrequency) + 1, _maxFftIndex);
            
            _CurrentBand._minimumFrequencyIndex = 
                Math.Min(_spectrumProvider.GetFftBandIndex(_CurrentBand._minimumFrequency), _maxFftIndex);

            int actualResolution = SpectrumResolution;

            int indexCount = _CurrentBand._maximumFrequencyIndex - _CurrentBand._minimumFrequencyIndex;
            double linearIndexBucketSize = Math.Round(indexCount / (double)actualResolution, 3);

            _spectrumIndexMax = _spectrumIndexMax.CheckBuffer(actualResolution, true);
            _spectrumLogScaleIndexMax = _spectrumLogScaleIndexMax.CheckBuffer(actualResolution, true);

            //?????? Isnt this one?
            //double maxLog = Math.Log(actualResolution, actualResolution);
            double maxLog = 1.0;

            for (int i = 1; i < actualResolution; i++)
            {
                int logIndex =
                    (int)((maxLog - Math.Log((actualResolution + 1) - i, (actualResolution + 1))) * indexCount) +
                    _CurrentBand._minimumFrequencyIndex;

                _spectrumIndexMax[i - 1] = _CurrentBand._minimumFrequencyIndex + (int)(i * linearIndexBucketSize);
                _spectrumLogScaleIndexMax[i - 1] = logIndex;
            }

            if (actualResolution > 0)
            {
                _spectrumIndexMax[_spectrumIndexMax.Length - 1] =
                    _spectrumLogScaleIndexMax[_spectrumLogScaleIndexMax.Length - 1] = _CurrentBand._maximumFrequencyIndex;
            }

            _BandFrecuencies[bandIndex] = _CurrentBand;
        }

        protected virtual SpectrumPointData[] CalculateSpectrumPoints(double maxValue, float[] fftBuffer, int bandIndex)
        {
            BandData _CurrentBand = _BandFrecuencies[bandIndex];

            var dataPoints = new List<SpectrumPointData>();

            double value0 = 0, value = 0;
            double lastValue = 0;
            double actualMaxValue = maxValue;
            int spectrumPointIndex = 0;

            for (int i = _CurrentBand._minimumFrequencyIndex; i <= _CurrentBand._maximumFrequencyIndex; i++)
            {
                switch (ScalingStrategy)
                {
                    case ScalingStrategy.Decibel:
                        value0 = (((20 * Math.Log10(fftBuffer[i])) - MinDbValue) / DbScale) * actualMaxValue;
                        break;
                    case ScalingStrategy.Linear:
                        value0 = (fftBuffer[i] * ScaleFactorLinear) * actualMaxValue;
                        break;
                    case ScalingStrategy.Sqrt:
                        value0 = ((Math.Sqrt(fftBuffer[i])) * ScaleFactorSqr) * actualMaxValue;
                        break;
                }

                bool recalc = true;

                value = Math.Max(0, Math.Max(value0, value));

                while (spectrumPointIndex <= _spectrumIndexMax.Length - 1 &&
                       i ==
                       (IsXLogScale
                           ? _spectrumLogScaleIndexMax[spectrumPointIndex]
                           : _spectrumIndexMax[spectrumPointIndex]))
                {
                    if (!recalc)
                        value = lastValue;

                    if (value > maxValue)
                        value = maxValue;

                    if (_useAverage && spectrumPointIndex > 0)
                        value = (lastValue + value) / 2.0;

                    dataPoints.Add(new SpectrumPointData {SpectrumPointIndex = spectrumPointIndex, Value = value});

                    lastValue = value;
                    value = 0.0;
                    spectrumPointIndex++;
                    recalc = false;
                }

                //value = 0;
            }

            return dataPoints.ToArray();
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !String.IsNullOrEmpty(propertyName))
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected struct SpectrumPointData
        {
            public int SpectrumPointIndex;
            public double Value;
        }
    }
}