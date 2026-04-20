using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;
using System;

public class ProductionController : IProductionController, ITickable, IDisposable
{
	private IPlayerModel playerModel;
	private SignalBus signalBus;
	private ResourceSettings resourceSettings;

	private Dictionary<int, SmelterAlloyData> smeltingData = new Dictionary<int, SmelterAlloyData>();
	private List<SmelterAlloyData> smeltData = new List<SmelterAlloyData>();

	private Dictionary<int, CrafterAlloyData> craftingData = new Dictionary<int, CrafterAlloyData>();
	private List<CrafterAlloyData> craftData = new List<CrafterAlloyData>();

	float smeltSpeed = 1;

	public ProductionController(SignalBus signalBus, IPlayerModel playerModel, ResourceSettings resourceSettings)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.resourceSettings = resourceSettings;

		this.signalBus.Subscribe<SmeltRecipeAddSignal>(OnRecipeAdded);
		this.signalBus.Subscribe<SmeltRecipeRemoveSignal>(OnRecipeRemoved);
		this.signalBus.Subscribe<CraftRecipeAddSignal>(OnCraftRecipeAdded);
		this.signalBus.Subscribe<CraftRecipeRemoveSignal>(OnCraftRecipeRemoved);
		this.signalBus.Subscribe<PlayerModelUpdatedSignal>(OnPlayerModelUpdated);
		this.signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	/// <summary>Unsubscribes from all signals.</summary>
	public void Dispose()
	{
		signalBus.Unsubscribe<SmeltRecipeAddSignal>(OnRecipeAdded);
		signalBus.Unsubscribe<SmeltRecipeRemoveSignal>(OnRecipeRemoved);
		signalBus.Unsubscribe<CraftRecipeAddSignal>(OnCraftRecipeAdded);
		signalBus.Unsubscribe<CraftRecipeRemoveSignal>(OnCraftRecipeRemoved);
		signalBus.Unsubscribe<PlayerModelUpdatedSignal>(OnPlayerModelUpdated);
		signalBus.Unsubscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	public void Tick()
	{
		Smelt();
		Craft();
	}

	private void OnRecipeAdded(SmeltRecipeAddSignal signal)
	{
		// Smelter already has some recipe
		if (smeltingData.ContainsKey(signal.SmelterId))
		{
			return;
		}

		AlloySmeltSettings settings = resourceSettings.GetSmeltSetting(signal.RecipeType);
		SmelterAlloyData newData = new SmelterAlloyData(signal.RecipeType, settings.TimeToSmelt);
		smeltingData.Add(signal.SmelterId, newData);

		if (playerModel.HasResource(AlloyToResourceConverter.ConvertToRaw(signal.RecipeType), resourceSettings.GetSmeltSetting(signal.RecipeType).ResourceNeeded))
		{
			AddSmelt(newData);
			playerModel.TryUseResource(AlloyToResourceConverter.ConvertToRaw(signal.RecipeType), resourceSettings.GetSmeltSetting(signal.RecipeType).ResourceNeeded);
		}
	}

	private void OnCraftRecipeAdded(CraftRecipeAddSignal signal)
	{
		// Smelter already has some recipe
		if (craftingData.ContainsKey(signal.SmelterId))
		{
			return;
		}

		ItemSmeltSettings settings = resourceSettings.GetItemSmeltSetting(signal.RecipeType);
		CrafterAlloyData newData = new CrafterAlloyData(signal.SmelterId, signal.RecipeType, settings.TimeToSmelt);
		craftingData.Add(signal.SmelterId, newData);
		
		if (playerModel.TryUseResources(settings.NeededResources))
		{
			AddCraft(newData);
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

	private void OnCraftRecipeRemoved(CraftRecipeRemoveSignal signal)
	{
		if (craftingData.ContainsKey(signal.SmelterId))
		{
			craftingData.Remove(signal.SmelterId);

			if (TryGetCraftData(signal.SmelterId, out CrafterAlloyData cData))
			{
				// Refund used resource if smelting in progress
				ResearchNeededResource[] requiredResources = resourceSettings.GetItemSmeltSetting(cData.Type).NeededResources;
				foreach (var requiredResource in requiredResources)
				{
					playerModel.AddResource(requiredResource.Type, requiredResource.Amount);
				}
				
				craftData.Remove(cData);
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

	private void Craft()
	{
		if (craftingData.Count <= 0)
		{
			return;
		}

		for (int i = 0; i < craftData.Count; i++)
		{
			CrafterAlloyData sad = craftData[i];
			sad.SmeltedTime += Time.deltaTime * smeltSpeed;
			if (sad.SmeltedTime >= sad.SmeltTime)
			{
				sad.SmeltedTime = 0;
				playerModel.AddResource(sad.Type, 1);

				// Remove if there is not enough raw material to smelt
				ResearchNeededResource[] requiredResources = resourceSettings.GetItemSmeltSetting(sad.Type).NeededResources;

				foreach(ResearchNeededResource res in requiredResources)
				{
					if (!playerModel.HasResource(sad.Type, res.Amount))
					{
						craftData.Remove(sad);
						sad.IsSmelting = false;
						return;
					}
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
		
		foreach (CrafterAlloyData sad in craftingData.Values)
		{
			ItemSmeltSettings settings = resourceSettings.GetItemSmeltSetting(sad.Type);
			if (!sad.IsSmelting && playerModel.TryUseResources(settings.NeededResources))
			{
				AddCraft(sad);
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

	private void AddCraft(CrafterAlloyData data)
	{
		craftData.Add(data);
		data.IsSmelting = true;
	}

	private bool TryGetCraftData(int id, out CrafterAlloyData data)
	{
		data = null;
		foreach (var cdata in craftData)
		{
			if (cdata.Id == id)
			{
				data = cdata;
				return true;
			}
		}
		
		return false;
	}

	public SmelterAlloyData GetAlloyData(int smelterId)
	{
		if (smeltingData.ContainsKey(smelterId))
		{
			return smeltingData[smelterId];
		}

		return null;
	}
	
	public CrafterAlloyData GetCraftingAlloyData(int smelterId)
	{
		if (craftingData.ContainsKey(smelterId))
		{
			return craftingData[smelterId];
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

	public void TryUnlockCrafter()
	{
		int nextCrafterId = playerModel.GetLastUnlockedCrafterId() + 1;
		int nextCrafterPrice = resourceSettings.GetCrafterSetting(nextCrafterId).Price;
		if (playerModel.HasMoney(nextCrafterPrice))
		{
			playerModel.UnlockCrafter(nextCrafterId);
			playerModel.UseMoney(nextCrafterPrice);
		}
	}
}
