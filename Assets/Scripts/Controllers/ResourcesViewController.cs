using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ResourcesViewController : IResourcesViewController, IDisposable
{
	private IPlayerModel playerModel;
	private SignalBus signalBus;
	
	public ResourcesViewController(IPlayerModel playerModel, SignalBus signalBus)
	{
		this.playerModel = playerModel;
		this.signalBus = signalBus;

		this.signalBus.Subscribe<PlayerModelUpdatedSignal>(OnResourcesUpdated);
		InitializeResources();
	}

	/// <summary>Unsubscribes from all signals.</summary>
	public void Dispose()
	{
		signalBus.Unsubscribe<PlayerModelUpdatedSignal>(OnResourcesUpdated);
	}

	private void OnResourcesUpdated(PlayerModelUpdatedSignal signal)
	{
		Dictionary<ResourceType, int> resourcesToUpdate = new Dictionary<ResourceType, int>();
		int resourcesAmount = playerModel.GetResource(signal.UpdatedResourceType);
		resourcesToUpdate.Add(signal.UpdatedResourceType, resourcesAmount);

		signalBus.Fire(new ResourcesViewUpdatedSignal() { ResourcesToUpdate = resourcesToUpdate });
	}

	private void InitializeResources()
	{
		Dictionary<ResourceType, int> playerResources = playerModel.GetResources();
		Dictionary<ResourceType, int> resources;

		if (playerResources != null && playerResources.Count > 0)
		{
			resources = playerResources;
		}
		else
		{
			resources = new Dictionary<ResourceType, int>();
		}

		signalBus.Fire(new ResourcesViewInitializedSignal() { ResourcesToInitialize = resources });
	}
}
