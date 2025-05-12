[System.Serializable]
public class CrafterAlloyData
{
	public ResourceType Type;
	public float SmeltTime;
	public float SmeltedTime;
	public bool IsSmelting;
	public int Id;

	public CrafterAlloyData(int id, ResourceType type, float smeltTime)
	{
		Id = id;
		Type = type;
		SmeltTime = smeltTime;
	}
}
