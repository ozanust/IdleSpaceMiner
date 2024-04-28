using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ProductionController : IProductionController, ITickable
{
	private IPlayerModel playerModel;
	private SignalBus signalBus;

	public ProductionController(SignalBus signalBus, PlanetSettings planetSettings, IPlayerModel playerModel, ISpaceModel spaceModel)
	{
		this.signalBus = signalBus;
		this.playerModel = playerModel;

		
	}

	public void Tick()
	{

	}
}
