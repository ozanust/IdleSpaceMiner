using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// Root serializable DTO that captures the full persistent game state.
/// </summary>
[Serializable]
public class SaveData
{
    public PlayerSaveData Player = new PlayerSaveData();
    public SpaceSaveData Space = new SpaceSaveData();
    public MiningSaveData Mining = new MiningSaveData();
    public SmeltingSaveData Smelting = new SmeltingSaveData();
    public CraftingSaveData Crafting = new CraftingSaveData();
}

[Serializable]
public class PlayerSaveData
{
    public int Money;
    public List<ResourceSaveEntry> Resources = new List<ResourceSaveEntry>();
    public List<CurrencySaveEntry> Currencies = new List<CurrencySaveEntry>();
    public List<AlloyType> UnlockedAlloys = new List<AlloyType>();
    public List<ResourceType> UnlockedItemRecipes = new List<ResourceType>();
    public List<ResearchType> UnlockedResearches = new List<ResearchType>();
    public int LastUnlockedSmelterId;
    public int LastUnlockedCrafterId;
}

[Serializable]
public class ResourceSaveEntry
{
    public ResourceType Type;
    public int Amount;
}

[Serializable]
public class CurrencySaveEntry
{
    public CurrencyType Type;
    public int Amount;
}

[Serializable]
public class SpaceSaveData
{
    public PlanetData[] Planets = new PlanetData[0];
}

[Serializable]
public class MiningSaveData
{
    /// <summary>
    /// Accumulated but not yet transferred mined amounts per planet.
    /// Key is PlanetIndex.
    /// </summary>
    public List<SaveEntry> PlanetMineEntries = new List<SaveEntry>();
}

[Serializable]
public class SaveEntry
{
    public int Id;
    public List<AmountEntry> Amounts = new List<AmountEntry>();
}

[Serializable]
public class AmountEntry
{
    public ResourceType Type;
    public float Amount;
}

[Serializable]
public class SmelterSaveData
{
    public int Id;
    public AlloyType TargetAlloy;
}

[Serializable]
public class CrafterSaveData
{
    public int Id;
    public ResourceType TargetResource;
}

[Serializable]
public class SmeltingSaveData
{
    public List<SmelterSaveData> SmeltEntries = new List<SmelterSaveData>();
}

[Serializable]
public class CraftingSaveData
{
    public List<CrafterSaveData> CraftEntries = new List<CrafterSaveData>();
}
