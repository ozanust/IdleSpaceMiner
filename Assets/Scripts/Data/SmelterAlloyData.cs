[System.Serializable]
public class SmelterAlloyData
{
	public AlloyType Type;
	public float SmeltTime;
	public float SmeltedTime;
	public bool IsSmelting;
	public int Id;

	public SmelterAlloyData(int id, AlloyType type, float smeltTime)
	{
		Id = id;
		Type = type;
		SmeltTime = smeltTime;
	}
}
