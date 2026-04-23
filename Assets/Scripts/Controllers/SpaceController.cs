using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

public class SpaceController : ISpaceController, ITickable, IDisposable
{
	PlanetSettings planetSettings;
	ISpaceModel spaceModel;
	IPlayerModel playerModel;
	GameSettings gameSettings;
	SignalBus signalBus;
	ISaveService saveService;
	IMiningController miningController;
	SaveController saveController;
	AsteroidView asteroidPrototype;
	MissileView missilePrototype;

	bool isAsteroidMinerUnlocked;
	int nextAsteroidSpawnTime;
	float asteroidSpawnTimer;
	int asteroidNumber = -1;

	public SpaceController(
		PlanetSettings planetSettings,
		ISpaceModel spaceModel,
		IPlayerModel playerModel,
		GameSettings gameSettings,
		SignalBus signalBus,
		ISaveService saveService,
		IMiningController miningController,
		SaveController saveController,
		AsteroidView asteroidView,
		MissileView missileView)
	{
		this.planetSettings = planetSettings;
		this.spaceModel = spaceModel;
		this.playerModel = playerModel;
		this.gameSettings = gameSettings;
		this.signalBus = signalBus;
		this.saveService = saveService;
		this.miningController = miningController;
		this.saveController = saveController;
		asteroidPrototype = asteroidView;
		missilePrototype = missileView;

		asteroidPrototype.gameObject.SetActive(false);
		missilePrototype.gameObject.SetActive(false);

		//OnRegister();
	}

	public void OnRegister()
	{
		SaveData save = saveService.Load();
		bool isNew = save == null;

		if (isNew)
		{
			spaceModel.InitializePlanetData(GetSampleData());
			playerModel.AddMoney(gameSettings.EconomySettings.StartingMoney);
		}
		else
		{
			saveController.BeginRestore();
			RestoreFromSave(save);
			saveController.EndRestore();
		}

		signalBus.Subscribe<ResearchCompletedSignal>(OnAsteroidResearchUnlocked);
		signalBus.Subscribe<AsteroidDestroyedSignal>(OnAsteroidDestroyed);
	}

	private void RestoreFromSave(SaveData save)
	{
		// --- Space state ---
		spaceModel.InitializePlanetData(save.Space.Planets);

		// --- Player money ---
		playerModel.AddMoney(save.Player.Money);

		// --- Resources ---
		foreach (ResourceSaveEntry entry in save.Player.Resources)
		{
			playerModel.AddResource(entry.Type, entry.Amount);
		}

		// --- Currencies ---
		foreach (CurrencySaveEntry entry in save.Player.Currencies)
		{
			playerModel.AddCurrency(entry.Type, entry.Amount);
		}

		// --- Unlocked alloys (skip defaults already added in PlayerModel ctor) ---
		AlloyType[] defaultAlloys = playerModel.GetUnlockedAlloys();
		foreach (AlloyType alloy in save.Player.UnlockedAlloys)
		{
			bool alreadyUnlocked = System.Array.IndexOf(defaultAlloys, alloy) >= 0;
			if (!alreadyUnlocked)
			{
				playerModel.UnlockAlloy(alloy);
			}
		}

		// --- Unlocked item recipes (skip defaults already added in PlayerModel ctor) ---
		ResourceType[] defaultRecipes = playerModel.GetUnlockedItemRecipes();
		foreach (ResourceType recipe in save.Player.UnlockedItemRecipes)
		{
			bool alreadyUnlocked = System.Array.IndexOf(defaultRecipes, recipe) >= 0;
			if (!alreadyUnlocked)
			{
				playerModel.UnlockItemRecipe(recipe);
			}
		}

		// --- Research ---
		foreach (ResearchType research in save.Player.UnlockedResearches)
		{
			playerModel.UnlockResearch(research);
		}

		// --- Smelter / crafter slot progression ---
		int[] unlockedSmelters = save.Player.UnlockedSmelters;
		for (int i = 0; i < unlockedSmelters.Length; i++)
		{
			playerModel.UnlockSmelter(unlockedSmelters[i]);
		}

		int[] unlockedCrafters = save.Player.UnlockedCrafters;
		for (int i = 0; i < unlockedCrafters.Length; i++)
		{
			playerModel.UnlockCrafter(unlockedCrafters[i]);
		}

		// --- Mining accumulated amounts ---
		RestoreMinedAmounts(save.Mining);

		// --- Asteroid miner active state ---
		if (playerModel.IsResearchUnlocked(ResearchType.AsteroidMiner))
		{
			nextAsteroidSpawnTime = Random.Range(
				gameSettings.GlobalSettings.AsteroidSpawnTimes[0],
				gameSettings.GlobalSettings.AsteroidSpawnTimes[1]);
			isAsteroidMinerUnlocked = true;
		}
		
		// --- Working smelters ---
		if (save.Smelting != null && save.Smelting.SmeltEntries.Count > 0)
		{
			foreach (var data in save.Smelting.SmeltEntries)
			{
				//playerModel.AddWorkingSmelter(data.Id, data.TargetAlloy);
				signalBus.Fire(new SmeltRecipeAddSignal() { RecipeType = data.TargetAlloy, SmelterId = data.Id });
			}
		}
		
		// --- Working crafters ---
		if (save.Crafting != null && save.Crafting.CraftEntries.Count > 0)
		{
			foreach (var data in save.Crafting.CraftEntries)
			{
				//playerModel.AddWorkingCrafter(data.Id, data.TargetResource);
				signalBus.Fire(new CraftRecipeAddSignal() { RecipeType = data.TargetResource, SmelterId = data.Id });
			}
		}
	}

	private void RestoreMinedAmounts(MiningSaveData miningData)
	{
		foreach (SaveEntry entry in miningData.PlanetMineEntries)
		{
			MiningData md;
			if (!miningController.TryGetMiningData(entry.Id, out md))
			{
				continue;
			}

			foreach (AmountEntry amountEntry in entry.Amounts)
			{
				foreach (ResourceMiningData pmd in md.MineDatas)
				{
					if (pmd.Type == amountEntry.Type)
					{
						pmd.MinedAmount = amountEntry.Amount;
						break;
					}
				}
			}
		}
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
		
		PlanetData data3 = new PlanetData();

		data3.IsUnraveled = true;
		data3.PlanetIndex = 2;
		data3.MiningRateLevel = 0;
		data3.ShipCargoLevel = 0;
		data3.ShipSpeedLevel = 0;
		
		PlanetData data4 = new PlanetData();

		data4.IsUnraveled = true;
		data4.PlanetIndex = 3;
		data4.MiningRateLevel = 0;
		data4.ShipCargoLevel = 0;
		data4.ShipSpeedLevel = 0;

		return new PlanetData[] { data, data2, data3, data4 };
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

	/// <summary>Unsubscribes from all signals.</summary>
	public void Dispose()
	{
		signalBus.Unsubscribe<ResearchCompletedSignal>(OnAsteroidResearchUnlocked);
		signalBus.Unsubscribe<AsteroidDestroyedSignal>(OnAsteroidDestroyed);
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
