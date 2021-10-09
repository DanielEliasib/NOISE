using System.Collections;
using System.Collections.Generic;

using AL.AudioSystem;
using Unity.Mathematics;

public class WaveProcessor
{
	private readonly LoopbackCapture _Loopback;

	private float[][] waves = null;
	private int currentWavelIndex;
	
	public WaveProcessor(in BandData bandData,in List<(DSPFilters, float)> filterData, int spectrumResolution, int nBuffer, int waveLenght)
	{
		_Loopback = new LoopbackCapture(spectrumResolution, ScalingStrategy.Sqrt, new List<BandData>(){bandData}, new List<(DSPFilters, float)>[] {filterData});
		_Loopback.StartListening();

		waves = new float[nBuffer][];
		for (int i = 0; i < waves.Length; i++)
		{
			waves[i] = new float[waveLenght];
		}

		currentWavelIndex = nBuffer - 1;
	}

	public void Update()
	{
		var waveData = _Loopback.SpectrumData[0];	//Should be of dim one

		if (waveData is null)
			return;

		ProcessRawWaveData(ref waveData);
	}

	private void ProcessRawWaveData(ref float[] waveData)
	{
		var first = waves[0];
		for (int i = 1; i < waves.Length; i++)
		{
			waves[i - 1] = waves[i];
		}

		waves[currentWavelIndex] = first;
		
		for (int i = 0; i < waveData.Length; i++)
		{
			//wave[i] =  waveData[i] + 0.2f;
			waves[currentWavelIndex][i] =  smoothValue(ref waveData, i, 4)*0.175f;
			//waves[currentWavelIndex][i] =  waveData[i]*0.2f;

			for (int j = 0; j < waves.Length-1; j++)
			{
				waves[currentWavelIndex][i] += waves[j][i];
			}

			waves[currentWavelIndex][i] = waves[currentWavelIndex][i] / waves.Length;
			//wave[i] = (wave[i] > 0.4f ? wave[i] - 0.35f : 0.0f)*0.25f;
		}
	}

	private float smoothValue(ref float[] waveData, int index, int level)
	{
		float val = 0;
		int count = 0;
		
		for (int i = -level / 2; i < level/2; i++)
		{
			int tempIndex = index - i;
			
			if(tempIndex < 0 || tempIndex >= waveData.Length)
				continue;

			val += waveData[tempIndex];
			count++;
		}

		return count > 0 ? val/count : 0;
	}
	
	public float[] GetWave()
	{
		return waves[currentWavelIndex];
	}
	
	public void Destroy()
	{
		_Loopback.StopListening();
	}
}
