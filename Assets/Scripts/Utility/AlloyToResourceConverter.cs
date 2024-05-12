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
				return ResourceType.CopperBar;
			case AlloyType.IronBar:
				return ResourceType.IronBar;
			case AlloyType.LeadBar:
				return ResourceType.LeadBar;
			case AlloyType.SilicaBar:
				return ResourceType.SilicaBar;
			case AlloyType.AluminumBar:
				return ResourceType.AluminumBar;
			default:
				return ResourceType.CopperBar;
		}
	}

	public static ResourceType ConvertToRaw(AlloyType alloyType)
	{
		switch (alloyType)
		{
			case AlloyType.CopperBar:
				return ResourceType.Copper;
			case AlloyType.IronBar:
				return ResourceType.Iron;
			case AlloyType.LeadBar:
				return ResourceType.Lead;
			case AlloyType.SilicaBar:
				return ResourceType.Silica;
			case AlloyType.AluminumBar:
				return ResourceType.Aluminum;
			default:
				return ResourceType.Copper;
		}
	}
}
