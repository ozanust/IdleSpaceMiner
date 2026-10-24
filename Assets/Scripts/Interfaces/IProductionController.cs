public interface IProductionController
{
	SmelterAlloyData GetAlloyData(int smelterId);
	CrafterAlloyData GetCraftingAlloyData(int smelterId);
	void TryUnlockSmelter();
	void TryUnlockCrafter();
	void SimulateOffline(float seconds);

	// Recipe values after research bonuses
	int GetSmeltResourceNeeded(AlloyType type);
	float GetSmeltTime(AlloyType type);
	float GetCraftTime(ResourceType type);
}