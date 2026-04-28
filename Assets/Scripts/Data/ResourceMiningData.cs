using System;

[System.Serializable]
public class ResourceMiningData
{
    public ResourceType Type;
	public float Ratio { get; private set; }
    public float MinedAmount;

	public ResourceMiningData(ResourceType type, float minedAmount, float ratio)
	{
		Type = type;
		MinedAmount = minedAmount;
		Ratio = ratio;
	}
}
