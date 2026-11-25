using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

public class CargoController : ICargoController, IDisposable
{
	private SignalBus signalBus;
	private PlanetSettings planetSettings;
	private ISpaceModel spaceModel;
	private CargoShipView cargoShipPrototype;
	private DiContainer container;
	private IManagerController managerController;
	private IMothershipController mothershipController;

	private Dictionary<int, CargoShipView> ships = new Dictionary<int, CargoShipView>();

    public CargoController(DiContainer container, SignalBus signalBus, CargoShipView cargoShipPrototype, PlanetSettings planetSettings, ISpaceModel spaceModel, IManagerController managerController, IMothershipController mothershipController)
	{
		this.mothershipController = mothershipController;
		this.managerController = managerController;
		this.signalBus = signalBus;
		this.cargoShipPrototype = cargoShipPrototype;
		this.planetSettings = planetSettings;
		this.spaceModel = spaceModel;
		this.container = container;

		this.cargoShipPrototype.gameObject.SetActive(false);
		this.signalBus.Subscribe<PlanetTransformSignal>(OnPlanetUnlocked);
		this.signalBus.Subscribe<PlanetUpdatedSignal>(OnPlanetUpdated);
		this.signalBus.Subscribe<ManagersUpdatedSignal>(OnManagersUpdated);
		this.signalBus.Subscribe<MothershipUpdatedSignal>(OnManagersUpdated);
	}

	/// <summary>Unsubscribes from all signals.</summary>
	public void Dispose()
	{
		signalBus.Unsubscribe<PlanetTransformSignal>(OnPlanetUnlocked);
		signalBus.Unsubscribe<PlanetUpdatedSignal>(OnPlanetUpdated);
		signalBus.Unsubscribe<ManagersUpdatedSignal>(OnManagersUpdated);
		signalBus.Unsubscribe<MothershipUpdatedSignal>(OnManagersUpdated);
	}

	private void OnPlanetUnlocked(PlanetTransformSignal signal)
	{
		SpawnCargoShipAndAssign(signal.PlanetId);
	}

	private void OnPlanetUpdated(PlanetUpdatedSignal signal)
	{
		if (ships.ContainsKey(signal.PlanetId))
		{
			CargoShipView ship = ships[signal.PlanetId];

			PlanetData planetData;
			if (spaceModel.TryGetPlanetData(signal.PlanetId, out planetData))
			{
				ApplyStats(ship, signal.PlanetId, planetData.CurrentShipSpeed, planetData.CurrentShipCargo);
			}
		}
	}

	// Managers and mothership rooms can change every ship's speed and cargo
	private void OnManagersUpdated()
	{
		foreach (KeyValuePair<int, CargoShipView> pair in ships)
		{
			PlanetData planetData;
			if (spaceModel.TryGetPlanetData(pair.Key, out planetData))
			{
				ApplyStats(pair.Value, pair.Key, planetData.CurrentShipSpeed, planetData.CurrentShipCargo);
			}
		}
	}

	private void ApplyStats(CargoShipView ship, int planetId, float speed, int cargo)
	{
		ship.SetShipSpeed(speed * GetShipSpeedMultiplier(planetId));
		ship.SetCargoSize(Mathf.RoundToInt(cargo * GetCargoMultiplier(planetId)));
	}

	private float GetShipSpeedMultiplier(int planetId)
	{
		return managerController.GetShipSpeedMultiplier(planetId) * mothershipController.GetMultiplier(RoomType.Aeronautical);
	}

	private float GetCargoMultiplier(int planetId)
	{
		return managerController.GetCargoMultiplier(planetId) * mothershipController.GetMultiplier(RoomType.Packaging);
	}

	private void SpawnCargoShipAndAssign(int planetId)
	{
		CargoShipView ship = container.InstantiatePrefabForComponent<CargoShipView>(cargoShipPrototype);

		PlanetData data;
		PlanetDataSetting setting;
		Transform planetTransform;

		if (spaceModel.TryGetPlanetData(planetId, out data) && planetSettings.TryGetPlanetSetting(planetId, out setting) && spaceModel.TryGetPlanetTransform(planetId, out planetTransform))
		{
			ApplyStats(ship, planetId, PlanetUpgradeFormula.GetShipSpeed(setting, data.ShipSpeedLevel), PlanetUpgradeFormula.GetShipCargo(setting, data.ShipCargoLevel));
			ship.SetTargetPlanet(planetTransform, planetId);
			ship.gameObject.SetActive(true);

			if (!ships.ContainsKey(planetId))
			{
				ships.Add(planetId, ship);
			}
		}
		else
		{
			Debug.LogError("Couldn't set ship speed and target, check planet data and planet data settings!");
			return;
		}
	}
}
