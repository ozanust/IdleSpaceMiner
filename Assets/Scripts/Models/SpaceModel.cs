using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SpaceModel : ISpaceModel
{
	private PlanetData[] planetData;
	private Dictionary<int, Transform> planetTransformDatas = new Dictionary<int, Transform>();
	readonly SignalBus signalBus;

	public SpaceModel(SignalBus signalBus)
	{
		this.signalBus = signalBus;

		signalBus.Subscribe<PlanetTransformSignal>(OnSetPlanetTransform);
	}

    public PlanetData[] GetPlanetsData()
	{
		return planetData;
	}

	public void InitializePlanetData(PlanetData[] planetData)
	{
		this.planetData = planetData;
		signalBus.Fire(new SpaceModelInitializedSignal() { Data = planetData });
	}

	public void UnlockPlanet(int planetIndex)
	{
		planetData[planetIndex].IsUnlocked = true;
		signalBus.Fire(new PlanetUnlockedSignal() { PlanetId = planetIndex });
	}

	public void UnravelPlanet(int planetIndex)
	{
		planetData[planetIndex].IsUnraveled = true;
		signalBus.Fire(new PlanetUnraveledSignal() { PlanetId = planetIndex });
	}

	private void OnSetPlanetTransform(PlanetTransformSignal signal)
	{
		if (!planetTransformDatas.ContainsKey(signal.PlanetId))
		{
			planetTransformDatas.Add(signal.PlanetId, signal.PlanetTransform);
		}
	}

	public bool TryGetPlanetTransform(int planetId, out Transform transform)
	{
		transform = null;
		if (!planetTransformDatas.ContainsKey(planetId))
		{
			Debug.LogWarning("Couldn't find planet transform!");
			return false;
		}

		transform = planetTransformDatas[planetId];
		return true;
	}

	public bool TryGetPlanetData(int planetId, out PlanetData data)
	{
		data = null;
		for (int i = 0; i < planetData.Length; i++)
		{
			if (planetData[i].PlanetIndex == planetId)
			{
				data = planetData[i];
				return true;
			}
		}

		return false;
	}
}
