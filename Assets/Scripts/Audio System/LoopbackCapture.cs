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
        private const FftSize _CFftSize = FftSize.Fft4096;
        public const int _MaxAudioValue = 10;

        WasapiLoopbackCapture _Capture;
        SoundInSource _SoundSource;

        BasicSpectrumProvider _BasicSpectrumProvider;
        LineSpectrum _LineSpectrum;

        int _SpectrumRes;
        private ScalingStrategy _ScalingStrategy;

        private SingleBlockNotificationStream _SingleBlockNotificationStream;

        IWaveSource _RealTimeSource;

        public float[] SpectrumData { get { return _SpectrumData; } }
        public float[] _SpectrumData;

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

            _LineSpectrum = new LineSpectrum(_CFftSize)
            {
                SpectrumProvider = _BasicSpectrumProvider,
                BarCount = _SpectrumRes,
                UseAverage = true,
                IsXLogScale = true,
                ScalingStrategy = _ScalingStrategy
            };

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
                
                float[] spectrumData = _LineSpectrum.GetSpectrumData(_MaxAudioValue);

                if (spectrumData != null)
                {
                    //OPTIMIZE
                    _SpectrumData = spectrumData.ToArray();
                    //ProcessSpectrum(ref spectrumData);
                    //Debug.Log("Data Size: " + _SpectrumData.Length);
                    //for (int i = 0; i < spectrumData.Length; i++)
                    //{
                    //    Debug.Log("Data[" + i + "]: " + spectrumData[i]);
                    //}
                }
            }
        }

        private void singleBlockNotificationStream_SingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            _BasicSpectrumProvider.Add(e.Left, e.Right);
        }

        private void ProcessSpectrum(ref float[] spectrum)
        {
            float postScaleAverage = 0.0f;
            float[] _PostScaled = new float[_SpectrumRes];

            float postScaleStep = 1.0f / _SpectrumRes;
            float postScaledPoint = postScaleStep;

            //Scale spectrum
            for (int i = 0; i < _SpectrumRes; i++)
            {
                if(i == 0)
                {
                    _PostScaled[i] = spectrum[i];
                }
                else
                {
                    float postScaleValue = postScaledPoint * spectrum[i] * (RealtimeAudio.MaxAudioValue - (1.0f - postScaledPoint));
                    _PostScaled[i] = Mathf.Clamp(postScaleValue, 0, RealtimeAudio.MaxAudioValue);
                }

                postScaledPoint += postScaleStep;
            }

            _SpectrumData = _PostScaled.ToArray();
        }
        
    }

    
}


