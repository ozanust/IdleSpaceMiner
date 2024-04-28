using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AlloyToResourceConverter
{
	public static ResourceType Convert(AlloyType alloyType)
	{
		switch (alloyType)
		{
			case AlloyType.CopperBar:
				return ResourceType.CooperBar;
			case AlloyType.IronBar:
				return ResourceType.IronBar;
			case AlloyType.LeadBar:
				return ResourceType.LeadBar;
			case AlloyType.SilicaBar:
				return ResourceType.SilicaBar;
			case AlloyType.AluminumBar:
				return ResourceType.AluminumBar;
			default:
				return ResourceType.CooperBar;
		}
	}
}
