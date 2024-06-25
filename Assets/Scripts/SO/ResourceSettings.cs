using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceSettings", menuName = "Settings/Resource", order = 1)]
public class ResourceSettings : ScriptableObject
{
	[SerializeField] private ResourceDataSetting[] resourceDataSettings;
	[SerializeField] private AlloyDataSetting[] alloyDataSettings;
	[SerializeField] private ItemDataSetting[] itemDataSettings;
	[SerializeField] private ResourceValueSetting[] resourceValueSettings;
	[SerializeField] private ResourceTypeSetting[] resourceTypeSettings;
	[SerializeField] private AlloySmeltTimeSettings[] alloySmeltTimeSettings;
	[SerializeField] private ItemSmeltTimeSettings[] itemSmeltTimeSettings;
	[SerializeField] private SmelterSetting[] smelterSettings;
	[SerializeField] private ResearchSettings[] researchSettings;

	public ResourceDataSetting[] ResourceDataSettings => resourceDataSettings;
	public AlloyDataSetting[] AlloySettings => alloyDataSettings;
	public ItemDataSetting[] ItemDataSettings => itemDataSettings;
	public ResourceValueSetting[] ValueSettings => resourceValueSettings;
	public ResourceTypeSetting[] TypeSettings => resourceTypeSettings;
	public AlloySmeltTimeSettings[] AlloySmeltSettings => alloySmeltTimeSettings;
	public ItemSmeltTimeSettings[] ItemSmeltTimeSettings => itemSmeltTimeSettings;
	public SmelterSetting[] SmelterSettings => smelterSettings;
	public ResearchSettings[] ResearchSettings => researchSettings;

	public int GetResourceValue(ResourceType type)
	{
		for (int i = 0; i < resourceValueSettings.Length; i++)
		{
			if (type == resourceValueSettings[i].Type)
			{
				return resourceValueSettings[i].Value;
			}
		}

		return 0;
	}

	public ResourceDataSetting GetResourceData(ResourceType type)
	{
		for (int i = 0; i < resourceDataSettings.Length; i++)
		{
			if (type == resourceDataSettings[i].Type)
			{
				return resourceDataSettings[i];
			}
		}

		return null;
	}

	public AlloyDataSetting GetAlloyData(AlloyType type)
	{
		for (int i = 0; i < alloyDataSettings.Length; i++)
		{
			if (type == alloyDataSettings[i].Type)
			{
				return alloyDataSettings[i];
			}
		}

		return null;
	}

	public ItemDataSetting GetItemData(ResourceType type)
	{
		for (int i = 0; i < itemDataSettings.Length; i++)
		{
			if (type == itemDataSettings[i].Type)
			{
				return itemDataSettings[i];
			}
		}

		return null;
	}

	public ResourceType[] GetMainResourceSubType(MainResourceType type)
	{
		for (int i = 0; i < resourceTypeSettings.Length; i++)
		{
			if (type == resourceTypeSettings[i].Type)
			{
				return resourceTypeSettings[i].SubTypes;
			}
		}

		return null;
	}

	public MainResourceType GetResourceParentType(ResourceType type)
	{
		for (int i = 0; i < resourceTypeSettings.Length; i++)
		{
			for (int j = 0; j < resourceTypeSettings[i].SubTypes.Length; j++)
			{
				if (resourceTypeSettings[i].SubTypes[j] == type)
				{
					return resourceTypeSettings[i].Type;
				}
			}
		}

		return MainResourceType.None;
	}

	public AlloySmeltTimeSettings GetSmeltSetting(AlloyType type)
	{
		for (int i = 0; i < alloySmeltTimeSettings.Length; i++)
		{
			if (type == alloySmeltTimeSettings[i].Type)
			{
				return alloySmeltTimeSettings[i];
			}
		}

		return null;
	}

	public ItemSmeltTimeSettings GetItemSmeltSetting(ResourceType type)
	{
		for (int i = 0; i < itemSmeltTimeSettings.Length; i++)
		{
			if (type == itemSmeltTimeSettings[i].Type)
			{
				return itemSmeltTimeSettings[i];
			}
		}

		return null;
	}

	public SmelterSetting GetSmelterSetting(int smelterId)
	{
		for (int i = 0; i < smelterSettings.Length; i++)
		{
			if (smelterId == smelterSettings[i].SmelterId)
			{
				return smelterSettings[i];
			}
		}

		return null;
	}

	public ResearchSettings GetResearchSetting(ResearchType type)
	{
		for (int i = 0; i < researchSettings.Length; i++)
		{
			if (type == researchSettings[i].Type)
			{
				return researchSettings[i];
			}
		}

		return null;
	}
}

[System.Serializable]
public class ResourceDataSetting
{
	public ResourceType Type;
	public string Name;
	public Sprite Icon;
}

[System.Serializable]
public class AlloyDataSetting
{
	public AlloyType Type;
	public string Name;
	public Sprite Icon;
}

[System.Serializable]
public class ItemDataSetting
{
	public ResourceType Type;
	public string Name;
	public Sprite Icon;
}

[System.Serializable]
public class ResourceValueSetting
{
	public ResourceType Type;
	public int Value;
}

[System.Serializable]
public class ResourceTypeSetting
{
	public MainResourceType Type;
	public ResourceType[] SubTypes;
}

[System.Serializable]
public class AlloySmeltTimeSettings
{
	public AlloyType Type;
	public int ResourceNeeded;
	public float TimeToSmelt;
	public int PriceToUnlock;
}

[System.Serializable]
public class ItemSmeltTimeSettings
{
	public ResourceType Type;
	public ResearchNeededResource[] NeededResources;
	public float TimeToSmelt;
	public int PriceToUnlock;
}

[System.Serializable]
public class SmelterSetting
{
	public int SmelterId;
	public int Price;
}

[System.Serializable]
public class ResearchSettings
{
	public string Name;
	public Sprite Icon;
	public string Description;
	public ResearchType Type;
	public ResearchType RequiredResearch;
	public ResearchNeededResource[] NeededResources;
}

[System.Serializable]
public class ResearchNeededResource
{
	public ResourceType Type;
	public int Amount;
}
