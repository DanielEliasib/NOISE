using System.Collections;
using System.Collections.Generic;

using AL.AudioSystem;
using Unity.Mathematics;

public class WaveProcessor
{
	private readonly LoopbackCapture _Loopback;

	private float[] wave = null;

	public WaveProcessor(in BandData bandData,in List<(DSPFilters, float)> filterData, int spectrumResolution, int nBuffer)
	{
		_Loopback = new LoopbackCapture(spectrumResolution, ScalingStrategy.Sqrt, new List<BandData>(){bandData}, new List<(DSPFilters, float)>[] {filterData});
		_Loopback.StartListening();
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
		if (wave?.Length != waveData.Length)
			wave = new float[waveData.Length];

		for (int i = 0; i < waveData.Length; i++)
		{
			//wave[i] =  waveData[i] + 0.2f;
			wave[i] =  smoothValue(ref waveData, i, 100) + 0.2f;
			wave[i] = (wave[i] > 0.4f ? wave[i] - 0.35f : 0.0f)*0.5f;
		}
	}

	private float smoothValue(ref float[] waveData, int index, int level)
	{
		float val = 0;
		int count = 0;
		
		for (int i = -level / 2; i < level; i++)
		{
			int tempIndex = index - i;
			
			if(tempIndex < 0 || tempIndex >= waveData.Length || waveData[tempIndex] > 2.0f)
				continue;

			val += waveData[tempIndex];
			count++;
		}

		return count > 0 ? val/count : 0;
	}
	
	public float[] GetWave()
	{
		return wave;
	}
	
	public void Destroy()
	{
		_Loopback.StopListening();
	}
}
