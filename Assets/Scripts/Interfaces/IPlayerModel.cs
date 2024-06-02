using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerModel
{
    bool TryUseCurrency(CurrencyType type, int amount);
    void AddCurrency(CurrencyType type, int amount);
    bool HasCurrency(CurrencyType type, int amount);
    int GetCurrency(CurrencyType type);
    bool TryUseResource(ResourceType type, int amount);
    bool HasResource(ResourceType type, int amount);
    bool HasResources(ResearchNeededResource[] data);
    int GetResource(ResourceType type);
    void AddResource(ResourceType type, int amount);
    void AddMoney(int amount);
    void UseMoney(int amount);
    bool HasMoney(int amount);
    int GetMoney();
    Dictionary<ResourceType, int> GetResources();
    void UnlockSmelter(int smelterId);
    int GetLastUnlockedSmelterId();
    void UnlockAlloy(AlloyType type);
    AlloyType[] GetUnlockedAlloys();
    void UnlockResearch(ResearchType type);
    ResearchType[] GetUnlockedResearchs();
    bool IsResearchUnlocked(ResearchType type);
    void SetTargetSmelter(int smelterId);
    int GetTargetSmelter();
}
