using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IResourcesService
{
	public event Action OnResourceUpdated
	{
		add { }
		remove { }
    }
	
	public Dictionary<ResourceType, int> Resources { get; }
	void AddResource(ResourceType type, int amount);
	bool UseResource(ResourceType type, int amount);
}
