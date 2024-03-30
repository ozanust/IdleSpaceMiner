using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUnitFactory
{
	public UnitView GetUnit(Unit unit);
}
