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
	private List<SmelterAlloyData> smeltData = new List<SmelterAlloyData>();

	float smeltSpeed = 1;

	public ProductionController(SignalBus signalBus, IPlayerModel playerModel, ResourceSettings resourceSettings)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.resourceSettings = resourceSettings;

		this.signalBus.Subscribe<SmeltRecipeAddSignal>(OnRecipeAdded);
		this.signalBus.Subscribe<SmeltRecipeRemoveSignal>(OnRecipeRemoved);
		this.signalBus.Subscribe<PlayerModelUpdatedSignal>(OnPlayerModelUpdated);
		this.signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	public void Tick()
	{
		Smelt();
	}

	private void OnRecipeAdded(SmeltRecipeAddSignal signal)
	{
		// Smelter already has the same recipe
		if (smeltingData.ContainsKey(signal.SmelterId))
		{
			return;
		}

		AlloySmeltTimeSettings settings = resourceSettings.GetSmeltSetting(signal.RecipeType);
		SmelterAlloyData newData = new SmelterAlloyData(signal.RecipeType, settings.TimeToSmelt);
		smeltingData.Add(signal.SmelterId, newData);

		if (playerModel.HasResource(AlloyToResourceConverter.ConvertToRaw(signal.RecipeType), resourceSettings.GetSmeltSetting(signal.RecipeType).ResourceNeeded))
		{
			AddSmelt(newData);
			playerModel.TryUseResource(AlloyToResourceConverter.ConvertToRaw(signal.RecipeType), resourceSettings.GetSmeltSetting(signal.RecipeType).ResourceNeeded);
		}
	}

	private void OnRecipeRemoved(SmeltRecipeRemoveSignal signal)
	{
		if (smeltingData.ContainsKey(signal.SmelterId))
		{
			smeltingData.Remove(signal.SmelterId);

			// Refund used resource if smelting in progress
			if (smeltData.Count > signal.SmelterId)
			{
				playerModel.AddResource(AlloyToResourceConverter.ConvertToRaw(smeltData[signal.SmelterId].Type), resourceSettings.GetSmeltSetting(smeltData[signal.SmelterId].Type).ResourceNeeded);
				smeltData.RemoveAt(signal.SmelterId);
			}
		}
	}

	private void Smelt()
	{
		if (smeltingData.Count <= 0)
		{
			return;
		}

		for (int i = 0; i < smeltData.Count; i++)
		{
			SmelterAlloyData sad = smeltData[i];
			sad.SmeltedTime += Time.deltaTime * smeltSpeed;
			if (sad.SmeltedTime >= sad.SmeltTime)
			{
				sad.SmeltedTime = 0;
				playerModel.AddResource(AlloyToResourceConverter.Convert(sad.Type), 1);

				// Remove if there is not enough raw material to smelt
				if (!playerModel.HasResource(AlloyToResourceConverter.ConvertToRaw(sad.Type), resourceSettings.GetSmeltSetting(sad.Type).ResourceNeeded))
				{
					smeltData.Remove(sad);
					sad.IsSmelting = false;
				}
			}
		}
	}

	private void OnPlayerModelUpdated(PlayerModelUpdatedSignal signal)
	{
		foreach (SmelterAlloyData sad in smeltingData.Values)
		{
			if (signal.UpdatedResourceType == AlloyToResourceConverter.ConvertToRaw(sad.Type) && !sad.IsSmelting && playerModel.HasResource(AlloyToResourceConverter.ConvertToRaw(sad.Type), resourceSettings.GetSmeltSetting(sad.Type).ResourceNeeded))
			{
				playerModel.TryUseResource(AlloyToResourceConverter.ConvertToRaw(sad.Type), resourceSettings.GetSmeltSetting(sad.Type).ResourceNeeded);
				AddSmelt(sad);
			}
		}
	}

	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		if (signal.ResearchType == ResearchType.AdvancedFurnace)
		{
			smeltSpeed = 1.2f;
		}
	}

	private void AddSmelt(SmelterAlloyData data)
	{
		smeltData.Add(data);
		data.IsSmelting = true;
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
			playerModel.UseMoney(nextSmelterPrice);
		}
	}
}
