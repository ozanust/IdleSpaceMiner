using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class PlanetInfoController : IPlanetInfoController
{
	private SignalBus signalBus;
	private ISpaceModel spaceModel;
	private IPlayerModel playerModel;

	public PlanetInfoController(SignalBus signalBus, ISpaceModel spaceModel, IPlayerModel playerModel)
	{
		this.signalBus = signalBus;
		this.spaceModel = spaceModel;
		this.playerModel = playerModel;
	}

	public void UpdateMiningRate(int planetId)
	{
		PlanetData planetData;
		if (spaceModel.TryGetPlanetData(planetId, out planetData))
		{
			if (TryPay(planetData.TotalMiningRateUpdatePrice))
			{
				spaceModel.UpdatePlanetMiningRate(planetId);
			}
		}
	}

	public void UpdateShipSpeed(int planetId)
	{
		PlanetData planetData;
		if (spaceModel.TryGetPlanetData(planetId, out planetData))
		{
			if (TryPay(planetData.ShipSpeedUpdatePrice))
			{
				spaceModel.UpdatePlanetShipSpeed(planetId);
			}
		}
	}

	public void UpdateCargoSize(int planetId)
	{
		PlanetData planetData;
		if (spaceModel.TryGetPlanetData(planetId, out planetData))
		{
			if (TryPay(planetData.ShipCargoUpdatePrice))
			{
				spaceModel.UpdatePlanetCargo(planetId);
			}
		}
	}

	private bool TryPay(long price)
	{
		bool canPay = playerModel.HasMoney(price);
		if (canPay)
		{
			playerModel.UseMoney(price);
		}

		signalBus.Fire(new PurchaseSignal() { Kind = PurchaseKind.Upgrade, Success = canPay, FailMessage = "Not enough money" });
		return canPay;
	}
}
