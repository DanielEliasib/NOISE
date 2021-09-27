using System.Collections;
using System.Collections.Generic;

using AL.AudioSystem;
using Unity.Mathematics;

public class WaveBaker
{
	private LoopbackCapture loopback;

	private float[] wave = null;
	
	List<BandData> bandData;
	List<(DSPFilters, float)>[] filterData;

	public WaveBaker(in BandData bandData,in List<(DSPFilters, float)> filterData, int spectrumResolution, int nBuffer)
	{
		loopback = new LoopbackCapture(spectrumResolution, ScalingStrategy.Sqrt, new List<BandData>(){bandData}, new List<(DSPFilters, float)>[] {filterData});
		loopback.StartListening();
	}

	public void Update()
	{
		var waveData = loopback.SpectrumData[0];	//Should be of dim one

		if (waveData is null)
			return;

		wave = waveData;
	}

	public float[] GetWave()
	{
		return wave;
	}
	
	public void Destroy()
	{
		loopback.StopListening();
	}
}
