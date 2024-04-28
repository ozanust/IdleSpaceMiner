using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceSettings", menuName = "Settings/Resource", order = 1)]
public class ResourceSettings : ScriptableObject
{
	[SerializeField] private ResourceDataSetting[] resourceDataSettings;
	[SerializeField] private ResourceValueSetting[] resourceValueSettings;
	[SerializeField] private ResourceTypeSetting[] resourceTypeSettings;
	[SerializeField] private AlloySmeltTimeSettings[] alloySmeltTimeSettings;

	public ResourceDataSetting[] Settings => resourceDataSettings;
	public ResourceValueSetting[] ValueSettings => resourceValueSettings;
	public ResourceTypeSetting[] TypeSettings => resourceTypeSettings;
	public AlloySmeltTimeSettings[] AlloySmeltSettings => alloySmeltTimeSettings;

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
}

[System.Serializable]
public class ResourceDataSetting
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
	public float TimeToSmelt;
	public int PriceToUnlock;
}
