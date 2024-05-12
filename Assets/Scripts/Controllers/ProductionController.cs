using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;

public class ProductionController : IProductionController, ITickable
{
	private IPlayerModel playerModel;
	private SignalBus signalBus;
	private ResourceSettings resourceSettings;

	private Dictionary<int, SmelterAlloyData> smeltingData = new Dictionary<int, SmelterAlloyData>();
	private Dictionary<AlloyType, SmelterAlloyData> data = new Dictionary<AlloyType, SmelterAlloyData>();
	// use only this, remove "data" dict above
	private List<SmelterAlloyData> smeltData = new List<SmelterAlloyData>();

	public ProductionController(SignalBus signalBus, IPlayerModel playerModel, ResourceSettings resourceSettings)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.resourceSettings = resourceSettings;

		this.signalBus.Subscribe<SmeltRecipeAddSignal>(OnRecipeAdded);
		this.signalBus.Subscribe<SmeltRecipeRemoveSignal>(OnRecipeRemoved);
	}

	public void Tick()
	{
		Smelt();
	}

	private void OnRecipeAdded(SmeltRecipeAddSignal signal)
	{
		AlloySmeltTimeSettings settings = resourceSettings.GetSmeltSetting(signal.RecipeType);
		smeltingData.Add(signal.SmelterId, new SmelterAlloyData(signal.RecipeType, settings.TimeToSmelt));
	}

	private void OnRecipeRemoved(SmeltRecipeRemoveSignal signal)
	{
		if (smeltingData.ContainsKey(signal.SmelterId))
		{
			// refund used resource if smelting in progress
			if (data.ContainsKey(smeltData[signal.SmelterId].Type))
			{
				playerModel.AddResource(AlloyToResourceConverter.Convert(smeltData[signal.SmelterId].Type), resourceSettings.GetSmeltSetting(smeltData[signal.SmelterId].Type).ResourceNeeded);
			}

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
			// This prevents to have more than one same type of smelting, fix this
			// Subscribe to player model resource update signal to assign available smelting, don't do in update method
			// Check the smelters that are selected this type of alloy and assign any available one
			if (!data.ContainsKey(sad.Type) && playerModel.HasResource(AlloyToResourceConverter.ConvertToRaw(sad.Type), resourceSettings.GetSmeltSetting(sad.Type).ResourceNeeded))
			{
				playerModel.TryUseResource(AlloyToResourceConverter.ConvertToRaw(sad.Type), resourceSettings.GetSmeltSetting(sad.Type).ResourceNeeded);
				AddSmelt(sad);
			}
		}

		for (int i = 0; i < smeltData.Count; i++)
		{
			SmelterAlloyData sad = smeltData[i];
			sad.SmeltedTime += Time.deltaTime;
			if (sad.SmeltedTime >= sad.SmeltTime)
			{
				sad.SmeltedTime = 0;
				playerModel.AddResource(AlloyToResourceConverter.Convert(sad.Type), 1);
				data.Remove(sad.Type);
				smeltData.Remove(sad);
			}
		}
	}

	private void AddSmelt(SmelterAlloyData data)
	{
		this.data.Add(data.Type, data);
		smeltData = this.data.Values.ToList();
	}

	public SmelterAlloyData GetAlloyData(int smelterId)
	{
		if (smeltingData.ContainsKey(smelterId))
		{
			return smeltingData[smelterId];
		}

		return null;
	}

	public void TryUnlockSmelter()
	{
		int nextSmelterId = playerModel.GetLastUnlockedSmelterId() + 1;
		int nextSmelterPrice = resourceSettings.GetSmelterSetting(nextSmelterId).Price;

		if (playerModel.HasMoney(nextSmelterPrice))
		{
			playerModel.UnlockSmelter(nextSmelterId);
		}
	}
}
