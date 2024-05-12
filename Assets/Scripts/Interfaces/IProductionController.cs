public interface IProductionController
{
	SmelterAlloyData GetAlloyData(int smelterId);
	void TryUnlockSmelter();
}