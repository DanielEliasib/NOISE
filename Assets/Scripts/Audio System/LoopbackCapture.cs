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

        public float[] SpectrumData { get { return _SpectrumData; } }
        public float[] _SpectrumData;

        List<BandData> _BandData;

        public LoopbackCapture(int spectrumResolution, ScalingStrategy scalingStrategy)
        {
            _Capture = new WasapiLoopbackCapture();
            _Capture.Initialize();
            // Not necesarry but it might be usefull to change the device
            //_soundIn.Device = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Console);
            _SoundSource = new SoundInSource(_Capture);

            _SpectrumRes = spectrumResolution;
            _ScalingStrategy = scalingStrategy;
        }
        
        public void StartListening()
        {
            _SpectrumData = new float[1];

            _Capture = new WasapiLoopbackCapture();
            _Capture.Initialize();

            _SoundSource = new SoundInSource(_Capture);

            _BasicSpectrumProvider = 
                new BasicSpectrumProvider(
                    _SoundSource.WaveFormat.Channels, 
                    _SoundSource.WaveFormat.SampleRate, 
                    _CFftSize);

            _InternalSpectrum = new float[(int)_CFftSize];

            _BandData = new List<BandData>()
            {
                new BandData()
                {
                    _maximumFrequency = 150,
                    _minimumFrequency = 60
                },
                new BandData()
                {
                    _maximumFrequency = 250,
                    _minimumFrequency = 200
                }
            };

            _BandSpectrum = new BandSpectrumProcessor(_CFftSize, _BandData)
            {
                SpectrumProvider = _BasicSpectrumProvider,
                BarCount = _SpectrumRes,
                UseAverage = true,
                IsXLogScale = true,
                ScalingStrategy = _ScalingStrategy
            };

            _BandSpectrum.AddFilter(new LowpassFilter(_SoundSource.WaveFormat.SampleRate, 200), 0);
            _BandSpectrum.AddFilter(new LowpassFilter(_SoundSource.WaveFormat.SampleRate, 300), 1);

            _Capture.Start();

            _SingleBlockNotificationStream = new SingleBlockNotificationStream(_SoundSource.ToSampleSource());

            _RealTimeSource = _SingleBlockNotificationStream.ToWaveSource();

            _SoundSource.DataAvailable += _SoundSource_DataAvailable;

            _SingleBlockNotificationStream.SingleBlockRead += singleBlockNotificationStream_SingleBlockRead;
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
                float[] spectrumData = _BandSpectrum.GetSpectrumData(_MaxAudioValue, 1);

                if (spectrumData != null)
                {
                    //OPTIMIZE
                    
                    _SpectrumData = spectrumData.ToArray();
                }
            }
        }

        private void singleBlockNotificationStream_SingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            _BasicSpectrumProvider.Add(e.Left, e.Right);
        }
        
    }

    
}


