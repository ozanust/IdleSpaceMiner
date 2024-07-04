using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceToAlloyConverter
{
	public static AlloyType Convert(ResourceType resourceType)
	{
		switch (resourceType)
		{
			case ResourceType.CopperBar:
				return AlloyType.CopperBar;
			case ResourceType.IronBar:
				return AlloyType.IronBar;
			case ResourceType.LeadBar:
				return AlloyType.LeadBar;
			case ResourceType.SilicaBar:
				return AlloyType.SilicaBar;
			case ResourceType.AluminumBar:
				return AlloyType.AluminumBar;
			default:
				return AlloyType.CopperBar;
		}
	}

	public static AlloyType ConvertFromRaw(ResourceType resourceType)
	{
		switch (resourceType)
		{
			case ResourceType.Copper:
				return AlloyType.CopperBar;
			case ResourceType.Iron:
				return AlloyType.IronBar;
			case ResourceType.Lead:
				return AlloyType.LeadBar;
			case ResourceType.Silica:
				return AlloyType.SilicaBar;
			case ResourceType.Aluminum:
				return AlloyType.AluminumBar;
			default:
				return AlloyType.CopperBar;
		}
	}

}
