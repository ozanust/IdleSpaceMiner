using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SpaceView : MonoBehaviour
{
    [Inject] ISpaceController spaceController;
    [Inject] ISpaceModel spaceModel;
	[Inject] SignalBus signalBus;
	[Inject] IPlayerModel playerModel;
	[Inject] PlanetSettings planetSettings;
 
	[SerializeField] private PlanetSpaceView[] planets;

	private bool isViewInitialized;

	private void Awake()
	{
		signalBus.Subscribe<SpaceModelInitializedSignal>(OnSpaceModelInitialized);
		signalBus.Subscribe<PlanetUnlockedSignal>(OnPlanetUnlocked);
	}

	private void Start()
	{
		Application.targetFrameRate = 60;

		if (!isViewInitialized)
		{
			InitializePlanets(planetSettings);
			InitializePlanetData(spaceModel.GetPlanetsData());
		}
	}

	private void OnSpaceModelUpdated(PlanetData[] data)
	{

	}

	private void OnSpaceModelInitialized(SpaceModelInitializedSignal signal)
	{
		InitializePlanetData(signal.Data);
	}

	private void OnPlanetUnlocked(PlanetUnlockedSignal signal)
	{
		planets[signal.PlanetId].Unlock();
		signalBus.Fire(new PlanetTransformSignal()
		{
			PlanetId = signal.PlanetId,
			PlanetTransform = planets[signal.PlanetId].transform
		});
	}

	private void InitializePlanets(PlanetSettings settings)
	{
		for (int i = 0; i < settings.Settings.Length; i++)
		{
			PlanetDataSetting setting = settings.Settings[i];
			planets[i].SetIndex(setting.PlanetId);
			planets[i].SetPrice(setting.PlanetPrice);
			planets[i].SetPlanetIcon(setting.Icon);
		}
	}

	private void InitializePlanetData(PlanetData[] data)
	{
		for (int i = 0; i < data.Length; i++)
		{
			int planetIndex = data[i].PlanetIndex;
			if (data[i].IsUnraveled)
			{
				planets[planetIndex].Unravel();
			}

			if (data[i].IsUnlocked)
			{
				planets[planetIndex].Unlock();
				// set planet data?
			}

			planets[planetIndex].OnClick.AddListener(OnClickPlanet);
		}

		isViewInitialized = true;
	}

	private void OnClickPlanet(int index)
	{
		spaceController.ClickPlanet(index);
	}
}
