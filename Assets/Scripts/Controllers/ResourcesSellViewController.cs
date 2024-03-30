using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ResourcesSellViewController : IResourcesSellViewController
{
	private SignalBus signalBus;
	private IPlayerModel playerModel;
	private ResourceSettings resourceSettings;

	public ResourcesSellViewController(SignalBus signalBus, IPlayerModel playerModel, ResourceSettings resourceSettings)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.resourceSettings = resourceSettings;

		this.signalBus.Subscribe<ResourcesSellSignal>(OnSellResourceSignal);
	}

	private void OnSellResourceSignal(ResourcesSellSignal signal)
	{
		if (playerModel.HasResource(signal.Type, signal.Amount))
		{
			playerModel.TryUseResource(signal.Type, signal.Amount);
			playerModel.AddMoney(signal.Amount * resourceSettings.GetResourceValue(signal.Type));
		}
	}
}
