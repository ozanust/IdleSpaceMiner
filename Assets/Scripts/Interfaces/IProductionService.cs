using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProductionService
{
	public event Action OnUnitsUpdated
	{
		add { }
		remove { }
	}

	public Dictionary<UnitType, List<UnitView>> Units { get; set; }
	void AddUnit(UnitType type, UnitView unitView);
}
