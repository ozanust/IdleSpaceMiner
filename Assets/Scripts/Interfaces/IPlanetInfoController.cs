using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlanetInfoController
{
	public void UpdateMiningRate(int planetId);
	public void UpdateShipSpeed(int planetId);
	public void UpdateCargoSize(int planetId);
}
