using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionService : IProductionService
{
	public event Action OnUnitsUpdated;

	private Dictionary<UnitType, List<UnitView>> units = new Dictionary<UnitType, List<UnitView>>();
	public Dictionary<UnitType, List<UnitView>> Units
	{
		get
		{
			return units;
		}

		set
		{
			units = value;
		}
	}

	public void AddUnit(UnitType type, UnitView unitView)
	{
		if (units.ContainsKey(type))
		{
			units[type].Add(unitView);
		}
		else
		{
			units.Add(type, new List<UnitView>() { unitView });
		}

		OnUnitsUpdated?.Invoke();
	}
}
