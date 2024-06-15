using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SpaceController : ISpaceController, ITickable
{
	PlanetSettings planetSettings;
	ISpaceModel spaceModel;
	IPlayerModel playerModel;
	GameSettings gameSettings;
	SignalBus signalBus;

	bool isNew;
	bool isAsteroidMinerUnlocked;
	int nextAsteroidSpawnTime;
	float asteroidSpawnTimer;

	public SpaceController(PlanetSettings planetSettings, ISpaceModel spaceModel, IPlayerModel playerModel, GameSettings gameSettings, SignalBus signalBus)
	{
		this.planetSettings = planetSettings;
		this.spaceModel = spaceModel;
		this.playerModel = playerModel;
		this.gameSettings = gameSettings;
		this.signalBus = signalBus;
		isNew = true;

		OnRegister();
	}

	private void OnRegister()
	{
		// read from save file
		// true for now
		if (isNew)
		{
			spaceModel.InitializePlanetData(GetSampleData());
			playerModel.AddMoney(gameSettings.EconomySettings.StartingMoney);
		}
		else
		{
			// read planet data from save file
		}

		signalBus.Subscribe<ResearchCompletedSignal>(OnAsteroidResearchUnlocked);
	}

	private PlanetData[] GetSampleData()
	{
		PlanetData data = new PlanetData();

		data.IsUnraveled = true;
		data.PlanetIndex = 0;
		data.MiningRateLevel = 0;
		data.ShipCargoLevel = 0;
		data.ShipSpeedLevel = 0;

		PlanetData data2 = new PlanetData();

		data2.IsUnraveled = true;
		data2.PlanetIndex = 1;
		data2.MiningRateLevel = 0;
		data2.ShipCargoLevel = 0;
		data2.ShipSpeedLevel = 0;

		return new PlanetData[] { data, data2 };
	}

	private void OnAsteroidResearchUnlocked(ResearchCompletedSignal signal)
	{
		if (signal.ResearchType == ResearchType.AsteroidMiner)
		{
			isAsteroidMinerUnlocked = true;
		}
	}

	public void OpenPlanet(int planetIndex)
	{
		signalBus.Fire(new PlanetOpenSignal() { PlanetId = planetIndex });
	}

	public void UnlockPlanet(int planetIndex)
	{
		spaceModel.UnlockPlanet(planetIndex);
	}

	public void UnravelPlanet(int planetIndex)
	{
		spaceModel.UnravelPlanet(planetIndex);
	}

	public void ClickPlanet(int planetIndex)
	{
		PlanetData[] planetData = spaceModel.GetPlanetsData();

		if (!planetData[planetIndex].IsUnraveled)
		{
			return;
		}

		if (!planetData[planetIndex].IsUnlocked)
		{
			if (playerModel.HasMoney(planetSettings.Settings[planetIndex].PlanetPrice))
			{
				spaceModel.UnlockPlanet(planetIndex);
				playerModel.UseMoney(planetSettings.Settings[planetIndex].PlanetPrice);
			}
			else
			{
				Debug.LogWarning("Insufficient funds!");
			}
		}
		else
		{
			OpenPlanet(planetIndex);
		}
	}

	public void Tick()
	{
		if (isAsteroidMinerUnlocked)
		{
			asteroidSpawnTimer += Time.deltaTime;

			if (asteroidSpawnTimer >= nextAsteroidSpawnTime)
			{
				// spawn asteroid
				nextAsteroidSpawnTime = Random.Range(gameSettings.GlobalSettings.AsteroidSpawnTimes[0], gameSettings.GlobalSettings.AsteroidSpawnTimes[1]);
				asteroidSpawnTimer = 0;
			}
		}
	}
}
