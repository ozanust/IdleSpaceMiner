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
	AsteroidView asteroidPrototype;
	MissileView missilePrototype;

	bool isNew;
	bool isAsteroidMinerUnlocked;
	int nextAsteroidSpawnTime;
	float asteroidSpawnTimer;
	int asteroidNumber = -1;

	public SpaceController(PlanetSettings planetSettings, ISpaceModel spaceModel, IPlayerModel playerModel, GameSettings gameSettings, SignalBus signalBus, AsteroidView asteroidView, MissileView missileView)
	{
		this.planetSettings = planetSettings;
		this.spaceModel = spaceModel;
		this.playerModel = playerModel;
		this.gameSettings = gameSettings;
		this.signalBus = signalBus;
		asteroidPrototype = asteroidView;
		missilePrototype = missileView;

		asteroidPrototype.gameObject.SetActive(false);
		missilePrototype.gameObject.SetActive(false);
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
		signalBus.Subscribe<AsteroidDestroyedSignal>(OnAsteroidDestroyed);
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
			nextAsteroidSpawnTime = Random.Range(gameSettings.GlobalSettings.AsteroidSpawnTimes[0], gameSettings.GlobalSettings.AsteroidSpawnTimes[1]);
			isAsteroidMinerUnlocked = true;
		}
	}

	private void OnAsteroidDestroyed(AsteroidDestroyedSignal signal)
	{
		ResourceData[] rewards = spaceModel.GetAsteroidRewards(signal.AsteroidId).Rewards;
		foreach (ResourceData rd in rewards)
		{
			playerModel.AddResource(rd.Type, rd.Amount);
		}

		spaceModel.RemoveAsteroidData(signal.AsteroidId);
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
				float angle = Random.Range(0f, Mathf.PI * 2);
				float x = Mathf.Cos(angle) * 10;
				float y = Mathf.Sin(angle) * 10;
				Vector3 position = new Vector3(x, y, 0f);

				AsteroidView asteroid = Object.Instantiate(asteroidPrototype, position, Quaternion.identity);
				asteroidNumber += 1;
				int id = asteroidNumber;
				asteroid.Init(signalBus, id);
				asteroid.OnClick.AddListener(OnClickAsteroid);
				asteroid.gameObject.SetActive(true);

				AsteroidData data = new AsteroidData(new ResourceData[] { new ResourceData(ResourceType.Copper, Mathf.RoundToInt(playerModel.GetResource(ResourceType.Copper) * 0.2f)), new ResourceData(ResourceType.Iron, Mathf.RoundToInt(playerModel.GetResource(ResourceType.Iron) * 0.2f)) });
				spaceModel.AddAsteroidData(data, id);
				
				nextAsteroidSpawnTime = Random.Range(gameSettings.GlobalSettings.AsteroidSpawnTimes[0], gameSettings.GlobalSettings.AsteroidSpawnTimes[1]);
				asteroidSpawnTimer = 0;
			}
		}
	}

	private void OnClickAsteroid(Vector3 asteroidPosition)
	{
		MissileView missile = Object.Instantiate(missilePrototype, new Vector3(0, 0, 0), Quaternion.identity);
		missile.Init(asteroidPosition);
		missile.gameObject.SetActive(true);
	}
}
