public interface IProductionController
{
	SmelterAlloyData GetAlloyData(int smelterId);
	CrafterAlloyData GetCraftingAlloyData(int smelterId);
	void TryUnlockSmelter();
	void TryUnlockCrafter();
	void SimulateOffline(float seconds);

	// Recipe values after research bonuses
	ResearchNeededResource[] GetSmeltIngredients(AlloyType type);
	float GetSmeltTime(AlloyType type);
	float GetCraftTime(ResourceType type);
}