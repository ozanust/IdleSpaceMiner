using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Orchestrates periodic auto-saving and exposes an explicit Save method.
/// Subscribes to relevant signals to trigger saves on meaningful state changes.
/// </summary>
public class SaveController : ITickable, IDisposable
{
    private const float AutoSaveInterval = 60f;

    private readonly ISaveService saveService;
    private readonly IPlayerModel playerModel;
    private readonly ISpaceModel spaceModel;
    private readonly IMiningController miningController;
    private readonly SignalBus signalBus;

    private float autoSaveTimer;

    /// <summary>
    /// When true, all signal-driven and timer-driven saves are suppressed.
    /// Set to true before restoring state from a save file, false once restore is complete.
    /// </summary>
    private bool isRestoring;

    public SaveController(
        ISaveService saveService,
        IPlayerModel playerModel,
        ISpaceModel spaceModel,
        IMiningController miningController,
        SignalBus signalBus)
    {
        this.saveService = saveService;
        this.playerModel = playerModel;
        this.spaceModel = spaceModel;
        this.miningController = miningController;
        this.signalBus = signalBus;
        
        //saveService.DeleteSave();

        /*signalBus.Subscribe<PlanetUpdatedSignal>(OnSaveTrigger);
        signalBus.Subscribe<PlanetUnlockedSignal>(OnSaveTrigger);
        signalBus.Subscribe<ResearchCompletedSignal>(OnSaveTrigger);
        signalBus.Subscribe<SmelterUnlockedSignal>(OnSaveTrigger);
        signalBus.Subscribe<ResourcesSellSignal>(OnSaveTrigger);
        signalBus.Subscribe<RecipeUnlockedSignal>(OnSaveTrigger);*/
        signalBus.Subscribe<OnApplicationQuitSignal>(OnApplicationQuit);
    }
    
    public void Dispose()
    {
        /*signalBus.Unsubscribe<PlanetUpdatedSignal>(OnSaveTrigger);
        signalBus.Unsubscribe<PlanetUnlockedSignal>(OnSaveTrigger);
        signalBus.Unsubscribe<ResearchCompletedSignal>(OnSaveTrigger);
        signalBus.Unsubscribe<SmelterUnlockedSignal>(OnSaveTrigger);
        signalBus.Unsubscribe<ResourcesSellSignal>(OnSaveTrigger);
        signalBus.Unsubscribe<RecipeUnlockedSignal>(OnSaveTrigger);*/
        signalBus.Unsubscribe<OnApplicationQuitSignal>(OnApplicationQuit);
    }

    /// <summary>Suppresses all saves until EndRestore is called.</summary>
    public void BeginRestore()
    {
        isRestoring = true;
    }

    /// <summary>Re-enables saves and immediately writes a fresh snapshot.</summary>
    public void EndRestore()
    {
        isRestoring = false;
        //Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    /// <summary>Builds and writes the full save snapshot to disk.</summary>
    public void Save()
    {
        if (isRestoring)
        {
            return;
        }
        
        SaveData data = BuildSaveData();
        saveService.Save(data);
    }

    public void Tick()
    {
        if (isRestoring)
        {
            return;
        }

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= AutoSaveInterval)
        {
            autoSaveTimer = 0f;
            Save();
        }
    }

    private void OnSaveTrigger()
    {
        Save();
    }

    private SaveData BuildSaveData()
    {
        SaveData data = new SaveData();

        data.Player = BuildPlayerSaveData();
        data.Space = BuildSpaceSaveData();
        data.Mining = BuildMiningSaveData();
        data.Smelting = BuildSmeltingSaveData();
        data.Crafting = BuildCraftingSaveData();

        return data;
    }

    private PlayerSaveData BuildPlayerSaveData()
    {
        PlayerSaveData playerData = new PlayerSaveData();

        playerData.Money = playerModel.GetMoney();
        playerData.LastUnlockedSmelterId = playerModel.GetLastUnlockedSmelterId();
        playerData.LastUnlockedCrafterId = playerModel.GetLastUnlockedCrafterId();
        playerData.UnlockedCrafters = playerModel.GetUnlockedCrafters();
        playerData.UnlockedSmelters = playerModel.GetUnlockedSmelters();

        // Aggregate delivered player resources and in-transit cargo into a single
        // centralized mothership snapshot, keyed by ResourceType to avoid duplicates.
        Dictionary<ResourceType, int> mothershipResources = new Dictionary<ResourceType, int>();

        Dictionary<ResourceType, int> deliveredResources = playerModel.GetResources();
        if (deliveredResources != null)
        {
            foreach (KeyValuePair<ResourceType, int> pair in deliveredResources)
            {
                mothershipResources[pair.Key] = pair.Value;
            }
        }

        Dictionary<ResourceType, int> inTransitResources = miningController.GetTransferData();
        foreach (KeyValuePair<ResourceType, int> pair in inTransitResources)
        {
            if (mothershipResources.ContainsKey(pair.Key))
            {
                mothershipResources[pair.Key] += pair.Value;
            }
            else
            {
                mothershipResources[pair.Key] = pair.Value;
            }
        }

        foreach (KeyValuePair<ResourceType, int> pair in mothershipResources)
        {
            playerData.Resources.Add(new ResourceSaveEntry { Type = pair.Key, Amount = pair.Value });
        }

        foreach (CurrencyType currencyType in System.Enum.GetValues(typeof(CurrencyType)))
        {
            int amount = playerModel.GetCurrency(currencyType);
            if (amount > 0)
            {
                playerData.Currencies.Add(new CurrencySaveEntry { Type = currencyType, Amount = amount });
            }
        }

        foreach (AlloyType alloy in playerModel.GetUnlockedAlloys())
        {
            playerData.UnlockedAlloys.Add(alloy);
        }

        foreach (ResourceType recipe in playerModel.GetUnlockedItemRecipes())
        {
            playerData.UnlockedItemRecipes.Add(recipe);
        }

        foreach (ResearchType research in playerModel.GetUnlockedResearchs())
        {
            playerData.UnlockedResearches.Add(research);
        }

        return playerData;
    }

    private SpaceSaveData BuildSpaceSaveData()
    {
        SpaceSaveData spaceData = new SpaceSaveData();
        spaceData.Planets = spaceModel.GetPlanetsData();
        return spaceData;
    }

    private MiningSaveData BuildMiningSaveData()
    {
        MiningSaveData miningData = new MiningSaveData();

        PlanetData[] planets = spaceModel.GetPlanetsData();
        if (planets == null)
        {
            return miningData;
        }

        // Only save resources still accumulating at each planet (not yet picked up by a ship).
        // In-transit resources are already folded into PlayerSaveData.Resources above.
        foreach (PlanetData planet in planets)
        {
            if (!planet.IsUnlocked)
            {
                continue;
            }

            MiningData md;
            if (miningController.TryGetMiningData(planet.PlanetIndex, out md))
            {
                SaveEntry entry = new SaveEntry { Id = planet.PlanetIndex };
                foreach (ResourceMiningData pmd in md.MineDatas)
                {
                    entry.Amounts.Add(new AmountEntry { Type = pmd.Type, Amount = pmd.MinedAmount });
                }
                miningData.PlanetMineEntries.Add(entry);
            }
        }

        return miningData;
    }

    private SmeltingSaveData BuildSmeltingSaveData()
    {
        SmeltingSaveData smeltingData = new SmeltingSaveData();

        foreach (var smelter in playerModel.GetWorkingSmelters())
        {
            SmelterSaveData data = new SmelterSaveData();
            data.Id = smelter.Key;
            data.TargetAlloy = smelter.Value;
            smeltingData.SmeltEntries.Add(data);
        }
        
        return smeltingData;
    }
    
    private CraftingSaveData BuildCraftingSaveData()
    {
        CraftingSaveData craftingSaveData = new CraftingSaveData();
        
        foreach (var crafter in playerModel.GetWorkingCrafters())
        {
            CrafterSaveData data = new CrafterSaveData();
            data.Id = crafter.Key;
            data.TargetResource = crafter.Value;
            craftingSaveData.CraftEntries.Add(data);
        }
        
        return craftingSaveData;
    }
}
