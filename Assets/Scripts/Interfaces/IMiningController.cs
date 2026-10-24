using System.Collections.Generic;

public interface IMiningController
{
	/// <summary>Research bonus applied on top of every planet's own mining rate.</summary>
	float MiningRateMultiplier { get; }

	/// <summary>Base sell value of everything all bought planets mine per second.</summary>
	float GetValuePerSecond();

	TransferResourcesData[] TransferResources(int planetId, int amount);
	bool TryGetMiningData(int planetId, out MiningData data);

	/// <summary>
	/// Returns all resources currently in transit on cargo ships, aggregated across all planets.
	/// Used to fold in-transit cargo into the centralized mothership save.
	/// </summary>
	Dictionary<ResourceType, int> GetTransferData();
}
