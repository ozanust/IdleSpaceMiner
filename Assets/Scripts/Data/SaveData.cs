using System;
using System.Collections.Generic;

/// <summary>
/// Root serializable DTO that captures the full persistent game state.
/// </summary>
[Serializable]
public class SaveData
{
    public PlayerSaveData Player = new PlayerSaveData();
    public SpaceSaveData Space = new SpaceSaveData();
    public MiningSaveData Mining = new MiningSaveData();
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
    public List<PlanetMineSaveEntry> PlanetMineEntries = new List<PlanetMineSaveEntry>();
}

[Serializable]
public class PlanetMineSaveEntry
{
    public int PlanetId;
    public List<MineAmountEntry> MinedAmounts = new List<MineAmountEntry>();
}

[Serializable]
public class MineAmountEntry
{
    public ResourceType Type;
    public float Amount;
}
