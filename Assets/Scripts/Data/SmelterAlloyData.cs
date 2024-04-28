using System;

[System.Serializable]
public class SmelterAlloyData
{
	public AlloyType Type;
	public float SmeltTime;
	public float SmeltedTime;

	public SmelterAlloyData(AlloyType type, float smeltTime)
	{
		Type = type;
		SmeltTime = smeltTime;
	}
}
