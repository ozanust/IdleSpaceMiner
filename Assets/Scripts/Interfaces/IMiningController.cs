using System.Collections.Generic;

public interface IMiningController
{
	TransferResourcesData[] TransferResources(int planetId, int amount);
	bool TryGetMiningData(int planetId, out MiningData data);

	/// <summary>
	/// Returns all resources currently in transit on cargo ships, aggregated across all planets.
	/// Used to fold in-transit cargo into the centralized mothership save.
	/// </summary>
	Dictionary<ResourceType, int> GetTransferData();
}
