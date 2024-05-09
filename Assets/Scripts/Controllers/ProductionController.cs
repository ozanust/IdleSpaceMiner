using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ProductionController : IProductionController, ITickable
{
	private IPlayerModel playerModel;
	private SignalBus signalBus;
	private ResourceSettings resourceSettings;

	private Dictionary<int, SmelterAlloyData> smeltingData = new Dictionary<int, SmelterAlloyData>();

	public ProductionController(SignalBus signalBus, IPlayerModel playerModel, ResourceSettings resourceSettings)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.resourceSettings = resourceSettings;

		this.signalBus.Subscribe<SmeltRecipeAddSignal>(OnRecipAdded);
		this.signalBus.Subscribe<SmeltRecipeRemoveSignal>(OnRecipeRemoved);
	}

	public void Tick()
	{
		Smelt();
	}

	private void OnRecipAdded(SmeltRecipeAddSignal signal)
	{
		AlloySmeltTimeSettings settings = resourceSettings.GetSmeltSetting(signal.RecipeType);
		smeltingData.Add(signal.SmelterId, new SmelterAlloyData(signal.RecipeType, settings.TimeToSmelt));
	}

	private void OnRecipeRemoved(SmeltRecipeRemoveSignal signal)
	{
		if (smeltingData.ContainsKey(signal.SmelterId))
		{
			smeltingData.Remove(signal.SmelterId);
		}
	}

	private void Smelt()
	{
		if (smeltingData.Count <= 0)
		{
			return;
		}

		foreach (SmelterAlloyData sad in smeltingData.Values)
		{
			sad.SmeltedTime += Time.deltaTime;
			if (sad.SmeltedTime >= sad.SmeltTime)
			{
				sad.SmeltedTime = 0;
				playerModel.TryUseResource(AlloyToResourceConverter.Convert(sad.Type), resourceSettings.GetSmeltSetting(sad.Type).ResourceNeeded);
				playerModel.AddResource(AlloyToResourceConverter.Convert(sad.Type), 1);
			}
		}
	}

	public SmelterAlloyData GetAlloyData(int smelterId)
	{
		if (smeltingData.ContainsKey(smelterId))
		{
			return smeltingData[smelterId];
		}

		return null;
	}
}
