public class CraftingData
{
	public CrafterAlloyData[] SmelterDatas;

	public CraftingData(CrafterAlloyData[] smelterDatas)
	{
		SmelterDatas = smelterDatas;
		Sort(SmelterDatas);
	}

	private void Sort(CrafterAlloyData[] arr)
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
