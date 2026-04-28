public interface IProductionController
{
	SmelterAlloyData GetAlloyData(int smelterId);
	CrafterAlloyData GetCraftingAlloyData(int smelterId);
	void TryUnlockSmelter();
	void TryUnlockCrafter();
}