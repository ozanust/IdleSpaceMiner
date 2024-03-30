using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitFactory : IUnitFactory
{
	private IUnitPool unitPool;

	public UnitFactory(IUnitPool unitPool)
	{
		this.unitPool = unitPool;
	}

	public UnitView GetUnit(Unit unit)
	{
		return unitPool.GetUnit(unit, new Vector3(1, 2, 1));
	}
}
