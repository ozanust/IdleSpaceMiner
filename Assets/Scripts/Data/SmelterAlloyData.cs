using System;

[System.Serializable]
public class SmelterAlloyData
{
	public AlloyType Type;
	public float SmeltTime;
	public float SmeltedAmount;

	public SmelterAlloyData(AlloyType type, float smeltedAmount, float smeltTime)
	{
		Type = type;
		SmeltedAmount = smeltedAmount;
		SmeltTime = smeltTime;
	}
}
