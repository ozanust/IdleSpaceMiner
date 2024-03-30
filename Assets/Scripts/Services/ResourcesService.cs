using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesService : IResourcesService
{
	public event Action OnResourceUpdated;

	private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
	public Dictionary<ResourceType, int> Resources 
	{ 
		get 
		{ 
			return resources; 
		} 
	}

	public void AddResource(ResourceType type, int amount)
	{
		if (resources.ContainsKey(type))
		{
			resources[type] += amount;
		}
		else
		{
			resources.Add(type, amount);
		}

		OnResourceUpdated?.Invoke();
	}

	public bool UseResource(ResourceType type, int amount)
	{
		if (resources.ContainsKey(type) && resources[type] >= amount)
		{
			resources[type] -= amount;
			OnResourceUpdated?.Invoke();
			return true;
		}

		return false;
	}
}
