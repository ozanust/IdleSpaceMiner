public interface IMiningController
{
	TransferResourcesData[] TransferResources(int planetId, int amount);
	bool TryGetMiningData(int planetId, out MiningData data);
}
