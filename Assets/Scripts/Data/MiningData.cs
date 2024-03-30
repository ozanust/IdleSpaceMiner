using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningData
{
	public float TotalMineRate;
	public PlanetMineData[] MineDatas;

	public MiningData(float mineRate, PlanetMineData[] planetMineDatas)
	{
		TotalMineRate = mineRate;
		MineDatas = planetMineDatas;

		Sort(MineDatas);
	}

	public MiningData(float mineRate, PlanetMiningYield[] planetMineYieldData)
	{
		TotalMineRate = mineRate;

		PlanetMineData[] planetMineDatas = new PlanetMineData[planetMineYieldData.Length];
		for (int i = 0; i < planetMineYieldData.Length; i++)
		{
			planetMineDatas[i] = new PlanetMineData(planetMineYieldData[i].Type, 0, planetMineYieldData[i].YieldRatio);
		}

		MineDatas = planetMineDatas;

		Sort(MineDatas);
	}

	private void Sort(PlanetMineData[] arr)
	{
		int n = arr.Length;
		ResourceType temp;

		for (int i = 0; i < n - 1; i++)
		{
			for (int j = 0; j < n - i - 1; j++)
			{
				if (arr[j].Type > arr[j + 1].Type)
				{
					temp = arr[j].Type;
					arr[j] = arr[j + 1];
					arr[j + 1].Type = temp;
				}
			}
		}
	}
}
