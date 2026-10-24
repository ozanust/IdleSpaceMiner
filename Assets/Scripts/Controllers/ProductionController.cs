using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System;

public class ProductionController : IProductionController, ITickable, IDisposable
{
	private IPlayerModel playerModel;
	private SignalBus signalBus;
	private ResourceSettings resourceSettings;

	// Recipes assigned to smelters/crafters; IsSmelting marks the ones whose input is already consumed
	private Dictionary<int, SmelterAlloyData> smeltingData = new Dictionary<int, SmelterAlloyData>();
	private Dictionary<int, CrafterAlloyData> craftingData = new Dictionary<int, CrafterAlloyData>();

	float smeltSpeed = 1;
	float craftSpeed = 1;
	float smeltIngredientMultiplier = 1;
	private IManagerController managerController;

	// Research speed combined with the managers' global smelt and craft bonuses
	private float EffectiveSmeltSpeed => smeltSpeed * managerController.SmeltSpeedMultiplier;
	private float EffectiveCraftSpeed => craftSpeed * managerController.CraftSpeedMultiplier;

	public ProductionController(SignalBus signalBus, IPlayerModel playerModel, ResourceSettings resourceSettings, IManagerController managerController)
	{
		this.managerController = managerController;
		this.signalBus = signalBus;
		this.playerModel = playerModel;
		this.resourceSettings = resourceSettings;

		this.signalBus.Subscribe<SmeltRecipeAddSignal>(OnRecipeAdded);
		this.signalBus.Subscribe<SmeltRecipeRemoveSignal>(OnRecipeRemoved);
		this.signalBus.Subscribe<CraftRecipeAddSignal>(OnCraftRecipeAdded);
		this.signalBus.Subscribe<CraftRecipeRemoveSignal>(OnCraftRecipeRemoved);
		this.signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	/// <summary>Unsubscribes from all signals.</summary>
	public void Dispose()
	{
		signalBus.Unsubscribe<SmeltRecipeAddSignal>(OnRecipeAdded);
		signalBus.Unsubscribe<SmeltRecipeRemoveSignal>(OnRecipeRemoved);
		signalBus.Unsubscribe<CraftRecipeAddSignal>(OnCraftRecipeAdded);
		signalBus.Unsubscribe<CraftRecipeRemoveSignal>(OnCraftRecipeRemoved);
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
		SmelterAlloyData newData = new SmelterAlloyData(signal.SmelterId, signal.RecipeType, settings.TimeToSmelt);
		smeltingData.Add(signal.SmelterId, newData);

		TryStartSmelt(newData);
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

		TryStartCraft(newData);
	}

	private void OnRecipeRemoved(SmeltRecipeRemoveSignal signal)
	{
		if (smeltingData.TryGetValue(signal.SmelterId, out SmelterAlloyData sData))
		{
			smeltingData.Remove(signal.SmelterId);

			// Refund used resource if smelting in progress
			if (sData.IsSmelting)
			{
				foreach (ResearchNeededResource ingredient in GetSmeltIngredients(sData.Type))
				{
					playerModel.AddResource(ingredient.Type, ingredient.Amount);
				}
			}
		}
	}

	private void OnCraftRecipeRemoved(CraftRecipeRemoveSignal signal)
	{
		if (craftingData.TryGetValue(signal.SmelterId, out CrafterAlloyData cData))
		{
			craftingData.Remove(signal.SmelterId);

			// Refund used resource if crafting in progress
			if (cData.IsSmelting)
			{
				ResearchNeededResource[] requiredResources = resourceSettings.GetItemSmeltSetting(cData.Type).NeededResources;
				foreach (var requiredResource in requiredResources)
				{
					playerModel.AddResource(requiredResource.Type, requiredResource.Amount);
				}
			}
		}
	}

	private void Smelt()
	{
		foreach (var sad in smeltingData.Values)
		{
			// Idle smelters wait until there is enough raw material again
			if (!sad.IsSmelting && !TryStartSmelt(sad))
			{
				continue;
			}

			sad.SmeltedTime += Time.deltaTime * EffectiveSmeltSpeed;
			if (sad.SmeltedTime >= sad.SmeltTime)
			{
				sad.SmeltedTime = 0;
				sad.IsSmelting = false;
				playerModel.AddResource(AlloyToResourceConverter.Convert(sad.Type), 1);

				TryStartSmelt(sad);
			}
		}
	}

	private void Craft()
	{
		foreach (var cad in craftingData.Values)
		{
			// Idle crafters wait until all required resources are available again
			if (!cad.IsSmelting && !TryStartCraft(cad))
			{
				continue;
			}

			cad.SmeltedTime += Time.deltaTime * EffectiveCraftSpeed;
			if (cad.SmeltedTime >= cad.SmeltTime)
			{
				cad.SmeltedTime = 0;
				cad.IsSmelting = false;
				playerModel.AddResource(cad.Type, 1);

				TryStartCraft(cad);
			}
		}
	}

	/// <summary>
	/// Runs smelters then crafters for the given time, each one in id order,
	/// so a machine can only use what earlier machines produced (same as Idle Planet Miner).
	/// </summary>
	public void SimulateOffline(float seconds)
	{
		foreach (var sad in GetOrdered(smeltingData))
		{
			float timeLeft = seconds * EffectiveSmeltSpeed;
			while (sad.IsSmelting || TryStartSmelt(sad))
			{
				float remaining = sad.SmeltTime - sad.SmeltedTime;
				if (timeLeft < remaining)
				{
					sad.SmeltedTime += timeLeft;
					break;
				}

				timeLeft -= remaining;
				sad.SmeltedTime = 0;
				sad.IsSmelting = false;
				playerModel.AddResource(AlloyToResourceConverter.Convert(sad.Type), 1);
			}
		}

		foreach (var cad in GetOrdered(craftingData))
		{
			float timeLeft = seconds * EffectiveCraftSpeed;
			while (cad.IsSmelting || TryStartCraft(cad))
			{
				float remaining = cad.SmeltTime - cad.SmeltedTime;
				if (timeLeft < remaining)
				{
					cad.SmeltedTime += timeLeft;
					break;
				}

				timeLeft -= remaining;
				cad.SmeltedTime = 0;
				cad.IsSmelting = false;
				playerModel.AddResource(cad.Type, 1);
			}
		}
	}

	private static List<T> GetOrdered<T>(Dictionary<int, T> data)
	{
		List<int> ids = new List<int>(data.Keys);
		ids.Sort();
		return ids.ConvertAll(id => data[id]);
	}

	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		switch (signal.ResearchType)
		{
			// Furnace upgrades stack
			case ResearchType.AdvancedFurnace:
			case ResearchType.SuperiorFurnace:
				smeltSpeed *= 1.2f;
				break;
			case ResearchType.AdvancedCrafter:
				craftSpeed = 1.2f;
				break;
			case ResearchType.SmeltingEfficiency:
				smeltIngredientMultiplier = 0.8f;
				break;
		}
	}

	/// <summary>
	/// What one bar costs: its ore for single ore bars, or the listed ingredients for bars
	/// made from other bars, reduced by Smelting Efficiency.
	/// </summary>
	public ResearchNeededResource[] GetSmeltIngredients(AlloyType type)
	{
		AlloySmeltSettings settings = resourceSettings.GetSmeltSetting(type);
		ResearchNeededResource[] baseIngredients = settings.NeededResources != null && settings.NeededResources.Length > 0
			? settings.NeededResources
			: new[] { new ResearchNeededResource { Type = AlloyToResourceConverter.ConvertToRaw(type), Amount = settings.ResourceNeeded } };

		ResearchNeededResource[] ingredients = new ResearchNeededResource[baseIngredients.Length];
		for (int i = 0; i < baseIngredients.Length; i++)
		{
			ingredients[i] = new ResearchNeededResource
			{
				Type = baseIngredients[i].Type,
				Amount = Mathf.CeilToInt(baseIngredients[i].Amount * smeltIngredientMultiplier)
			};
		}

		return ingredients;
	}

	public float GetSmeltTime(AlloyType type)
	{
		return resourceSettings.GetSmeltSetting(type).TimeToSmelt / EffectiveSmeltSpeed;
	}

	public float GetCraftTime(ResourceType type)
	{
		return resourceSettings.GetItemSmeltSetting(type).TimeToSmelt / EffectiveCraftSpeed;
	}

	/// <summary>Consumes the ingredients for one alloy and marks the smelter as working.</summary>
	private bool TryStartSmelt(SmelterAlloyData data)
	{
		if (!playerModel.TryUseResources(GetSmeltIngredients(data.Type)))
		{
			return false;
		}

		data.IsSmelting = true;
		return true;
	}

	/// <summary>Consumes the ingredients for one item and marks the crafter as working.</summary>
	private bool TryStartCraft(CrafterAlloyData data)
	{
		if (!playerModel.TryUseResources(resourceSettings.GetItemSmeltSetting(data.Type).NeededResources))
		{
			return false;
		}

		data.IsSmelting = true;
		return true;
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
		SmelterSetting nextSmelter = resourceSettings.GetSmelterSetting(nextSmelterId);
		if (nextSmelter == null)
		{
			return;
		}

		long nextSmelterPrice = nextSmelter.Price;
		if (playerModel.HasMoney(nextSmelterPrice))
		{
			playerModel.UnlockSmelter(nextSmelterId);
			playerModel.UseMoney(nextSmelterPrice);
		}
	}

	public void TryUnlockCrafter()
	{
		int nextCrafterId = playerModel.GetLastUnlockedCrafterId() + 1;
		CrafterSetting nextCrafter = resourceSettings.GetCrafterSetting(nextCrafterId);
		if (nextCrafter == null)
		{
			return;
		}

		long nextCrafterPrice = nextCrafter.Price;
		if (playerModel.HasMoney(nextCrafterPrice))
		{
			playerModel.UnlockCrafter(nextCrafterId);
			playerModel.UseMoney(nextCrafterPrice);
		}
	}
}
