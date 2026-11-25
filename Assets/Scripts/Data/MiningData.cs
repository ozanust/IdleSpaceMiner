using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningData
{
	public float TotalMineRate;
	public ResourceMiningData[] MineDatas;

	public MiningData(float mineRate, ResourceMiningData[] planetMineDatas)
	{
		TotalMineRate = mineRate;
		MineDatas = planetMineDatas;

		Sort(MineDatas);
	}

	public MiningData(float mineRate, PlanetMiningYield[] planetMineYieldData)
	{
		TotalMineRate = mineRate;

		ResourceMiningData[] planetMineDatas = new ResourceMiningData[planetMineYieldData.Length];
		for (int i = 0; i < planetMineYieldData.Length; i++)
		{
			planetMineDatas[i] = new ResourceMiningData(planetMineYieldData[i].Type, 0, planetMineYieldData[i].YieldRatio);
		}

		MineDatas = planetMineDatas;

		Sort(MineDatas);
	}

	// Ores in type order. The old hand-written sort copied references, so planets listing
	// their ores out of order ended up mining one ore several times and the others not at all
	private void Sort(ResourceMiningData[] arr)
	{
		System.Array.Sort(arr, (a, b) => a.Type.CompareTo(b.Type));
	}
}
