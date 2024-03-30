using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class CargoController : ICargoController
{
	private SignalBus signalBus;
	private PlanetSettings planetSettings;
	private ISpaceModel spaceModel;
	private CargoShipView cargoShipPrototype;

    public CargoController(SignalBus signalBus, CargoShipView cargoShipPrototype, PlanetSettings planetSettings, ISpaceModel spaceModel)
	{
		this.signalBus = signalBus;
		this.cargoShipPrototype = cargoShipPrototype;
		this.planetSettings = planetSettings;
		this.spaceModel = spaceModel;

		this.cargoShipPrototype.gameObject.SetActive(false);
		this.signalBus.Subscribe<PlanetTransformSignal>(OnPlanetUnlocked);
	}

	private void OnPlanetUnlocked(PlanetTransformSignal signal)
	{
		SpawnCargoShipAndAssign(signal.PlanetId);
	}

	private void SpawnCargoShipAndAssign(int planetId)
	{
		CargoShipView ship = Object.Instantiate(cargoShipPrototype);

		PlanetData data;
		PlanetDataSetting setting;
		Transform planetTransform;

		if (spaceModel.TryGetPlanetData(planetId, out data) && planetSettings.TryGetPlanetSetting(planetId, out setting) && spaceModel.TryGetPlanetTransform(planetId, out planetTransform))
		{
			float shipSpeed = setting.StartingShipSpeed * Mathf.Pow(setting.ShipSpeedIncreaseMultiplier, data.ShipSpeedLevel);
			int cargoSize = Mathf.FloorToInt(setting.StartingShipCargo * Mathf.Pow(setting.CargoIncreaseMultiplier, data.ShipCargoLevel));
			ship.SetShipSpeed(shipSpeed);
			ship.SetCargoSize(cargoSize);
			ship.SetTargetPlanet(planetTransform, planetId);
			ship.SetSignalBus(signalBus);
			ship.gameObject.SetActive(true);
		}
		else
		{
			Debug.LogError("Couldn't set ship speed and target, check planet data and planet data settings!");
			return;
		}
	}
}
