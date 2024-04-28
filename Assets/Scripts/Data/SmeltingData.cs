using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmeltingData
{
	public SmelterAlloyData[] SmelterDatas;

	public SmeltingData(SmelterAlloyData[] smelterDatas)
	{
		SmelterDatas = smelterDatas;
		Sort(SmelterDatas);
	}

	private void Sort(SmelterAlloyData[] arr)
	{
		int n = arr.Length;
		AlloyType temp;

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
