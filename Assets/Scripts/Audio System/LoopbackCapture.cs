using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CSCore;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using Assets.Scripts.Audio;
using System;
using System.Linq;

namespace AL.AudioSystem
{
    public enum DSPFilters
    {
        LowPass,
        HighPass,
    }

    public class LoopbackCapture
    {
        private const FftSize _CFftSize = FftSize.Fft8192;
        public const int _MaxAudioValue = 10;

        WasapiLoopbackCapture _Capture;
        SoundInSource _SoundSource;

        BasicSpectrumProvider _BasicSpectrumProvider;
        BandSpectrumProcessor _BandSpectrum;

        int _SpectrumRes;
        private ScalingStrategy _ScalingStrategy;

        private SingleBlockNotificationStream _SingleBlockNotificationStream;

        private float[] _InternalSpectrum;

        IWaveSource _RealTimeSource;

        public float[][] SpectrumData { get; private set; }

        private List<BandData> _BandData;
        private Dictionary<int, List<BiQuad>> _Filters;

        private List<(DSPFilters, float)>[] _FilterData;

        public LoopbackCapture(int spectrumResolution, ScalingStrategy scalingStrategy, List<BandData> bandData, List<(DSPFilters, float)>[] filters)
        {
            _Capture = new WasapiLoopbackCapture();
            _Capture.Initialize();
            // Not necesarry but it might be usefull to change the device
            //_soundIn.Device = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Console);
            _SoundSource = new SoundInSource(_Capture);

            _SpectrumRes = spectrumResolution;
            _ScalingStrategy = scalingStrategy;

            _BandData = bandData;
            
            _Filters = new Dictionary<int, List<BiQuad>>();

            _FilterData = filters;

        }
        
        public void StartListening()
        {
            _Capture = new WasapiLoopbackCapture();
            _Capture.Initialize();

            _SoundSource = new SoundInSource(_Capture);

            _BasicSpectrumProvider = 
                new BasicSpectrumProvider(
                    _SoundSource.WaveFormat.Channels, 
                    _SoundSource.WaveFormat.SampleRate, 
                    _CFftSize);

            _InternalSpectrum = new float[(int)_CFftSize];

            for (int i = 0; i < _FilterData.Length; i++)
            {
                List<BiQuad> currentFilters = new List<BiQuad>();
                for (int j = 0; j < _FilterData[i].Count; j++)
                {
                    BiQuad filter = null;
                    (var filterType, var frec) = _FilterData[i][j];
                    switch (filterType)
                    {
                        case DSPFilters.LowPass:
                            filter = new LowpassFilter(_SoundSource.WaveFormat.SampleRate, frec);
                            break;
                        case DSPFilters.HighPass:
                            filter = new HighpassFilter(_SoundSource.WaveFormat.SampleRate, frec);
                            break;
                        default:
                            break;
                    }

                    if(filter != null)
                    {
                        currentFilters.Add(filter);
                    }
                }

                _Filters.Add(i, currentFilters);
            }

            _BandSpectrum = new BandSpectrumProcessor(_CFftSize, _BandData)
            {
                SpectrumProvider = _BasicSpectrumProvider,
                BarCount = _SpectrumRes,
                UseAverage = true,
                IsXLogScale = true,
                ScalingStrategy = _ScalingStrategy
            };

            foreach(var filterPair in _Filters)
            {
                var currentList = filterPair.Value;
                for (int i = 0; i < currentList.Count; i++)
                {
                    _BandSpectrum.AddFilter(currentList[i], filterPair.Key);
                }
            }

            _Capture.Start();

            _SingleBlockNotificationStream = new SingleBlockNotificationStream(_SoundSource.ToSampleSource());

            _RealTimeSource = _SingleBlockNotificationStream.ToWaveSource();

            _SoundSource.DataAvailable += _SoundSource_DataAvailable;

            _SingleBlockNotificationStream.SingleBlockRead += singleBlockNotificationStream_SingleBlockRead;

            SpectrumData = new float[_BandData.Count][];
        }

        public void StopListening()
        {
            _SingleBlockNotificationStream.SingleBlockRead -= singleBlockNotificationStream_SingleBlockRead;

            _SoundSource.Dispose();
            _RealTimeSource.Dispose();
            _Capture.Stop();
            _Capture.Dispose();
        }

        //Private Methods
        private void _SoundSource_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            
            byte[] buffer = new byte[_RealTimeSource.WaveFormat.BytesPerSecond / 2];

            while (_RealTimeSource.Read(buffer, 0, buffer.Length) > 0)
            {
                for(int i  = 0; i < SpectrumData.Length; i++)
                {
                    float[] spectrumData = _BandSpectrum.GetSpectrumData(_MaxAudioValue, i);

                    if (spectrumData != null)
                    {
                        //OPTIMIZE
                        SpectrumData[i] = spectrumData.ToArray();
                    }
                }
            }
        }

        private void singleBlockNotificationStream_SingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            _BasicSpectrumProvider.Add(e.Left, e.Right);
        }
        
    }

    
}


