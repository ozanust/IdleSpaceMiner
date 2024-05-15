using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BindingsInstaller : MonoInstaller
{
	public override void InstallBindings()
	{
		InstallSignals();

		Container.Bind<GameSettings>().FromScriptableObjectResource("Settings/GameSettings").AsSingle();
		Container.Bind<ResourceSettings>().FromScriptableObjectResource("Settings/ResourceSettings").AsSingle();
		Container.Bind<PlanetSettings>().FromScriptableObjectResource("Settings/PlanetSettings").AsSingle();
		Container.Bind<CargoShipView>().FromComponentInNewPrefabResource("Game/CargoShip").AsSingle();
		Container.Bind<IResourcesService>().To<ResourcesService>().AsSingle();
		Container.Bind<IPlayerModel>().To<PlayerModel>().AsSingle();
		Container.Bind<ISpaceModel>().To<SpaceModel>().AsSingle();
		Container.Bind<ISpaceController>().To<SpaceController>().AsSingle();
		Container.Bind(typeof(IMiningController), typeof(ITickable)).To<MiningController>().AsSingle();
		Container.Bind<ICargoController>().To<CargoController>().AsSingle().NonLazy();
		Container.Bind<IResourcesViewController>().To<ResourcesViewController>().AsSingle().NonLazy();
		Container.Bind<IResourcesSellViewController>().To<ResourcesSellViewController>().AsSingle().NonLazy();
		Container.Bind<IPlanetInfoController>().To<PlanetInfoController>().AsSingle();
		Container.Bind(typeof(IProductionController), typeof(ITickable)).To<ProductionController>().AsSingle().NonLazy();
	}

	void InstallSignals()
	{
		SignalBusInstaller.Install(Container);
		Container.DeclareSignal<BindingsDoneSignal>();
		Container.DeclareSignal<SpaceModelInitializedSignal>();

		Container.DeclareSignal<PlayerModelUpdatedSignal>();
		Container.DeclareSignal<PlayerMoneyUpdatedSignal>();
		Container.DeclareSignal<PlayerDarkMatterUpdatedSignal>();

		Container.DeclareSignal<PlanetUnraveledSignal>();
		Container.DeclareSignal<PlanetUnlockedSignal>();
		Container.DeclareSignal<PlanetOpenSignal>();
		Container.DeclareSignal<PlanetUpdatedSignal>();
		Container.DeclareSignal<PlanetTransformSignal>();

		Container.DeclareSignal<CargoShipPlanetArrivalSignal>();
		Container.DeclareSignal<CargoShipMothershipArrivalSignal>();

		Container.DeclareSignal<ResourcesViewInitializedSignal>();
		Container.DeclareSignal<ResourcesViewUpdatedSignal>();
		Container.DeclareSignal<ResourcesSellSignal>();
		Container.DeclareSignal<ResourceDeselectedToSellSignal>();
		Container.DeclareSignal<ResourceSelectedToSellSignal>();
		Container.DeclareSignal<ResourcesViewClosedSignal>();

		Container.DeclareSignal<SmeltRecipeAddSignal>();
		Container.DeclareSignal<SmeltRecipeRemoveSignal>();
		Container.DeclareSignal<SmelterUnlockedSignal>();
		
		Container.DeclareSignal<ResearchCompletedSignal>();
		Container.DeclareSignal<MenuOpenSignal>();
	}
}
