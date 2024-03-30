using System;

[System.Serializable]
public class PlanetMineData
{
    public ResourceType Type;
	public float Ratio { get; private set; }
    public float MinedAmount;

	public PlanetMineData(ResourceType type, float minedAmount, float ratio)
	{
		Type = type;
		MinedAmount = minedAmount;
		Ratio = ratio;
	}
}
