using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProductionController
{
	public void Build(UnitType type);
	void Build(UnitType type, Vector3 rallyPoint);
}
