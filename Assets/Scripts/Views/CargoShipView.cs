using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class CargoShipView : MonoBehaviour
{
	SignalBus signalBus;

	private float shipSpeed;
	private int cargoSize;
	private int targetPlanetId;
	private Vector3 currentTarget;
	// turn this into vector3 if transform is not useful for tween functions
	private Transform targetPlanet;
	private Vector3 mothershipDefaultPosition = new Vector3(0, 0, 0);

	public void SetSignalBus(SignalBus signalBus)
	{
		this.signalBus = signalBus;
	}

	public void SetTargetPlanet(Transform targetPlanet, int targetPlanetId)
	{
		currentTarget = targetPlanet.position;
		this.targetPlanet = targetPlanet;
		this.targetPlanetId = targetPlanetId;
	}

	public void SetShipSpeed(float newSpeed)
	{
		shipSpeed = newSpeed;
	}

	public void SetCargoSize(int newCargo)
	{
		cargoSize = newCargo;
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.tag == "Planet")
		{
			currentTarget = mothershipDefaultPosition;
			signalBus.Fire(new CargoShipPlanetArrivalSignal() { PlanetId = targetPlanetId, CargoCapacity = cargoSize });
		}
		else if (collision.gameObject.tag == "Mothership")
		{
			currentTarget = targetPlanet.position;
			signalBus.Fire(new CargoShipMothershipArrivalSignal() { PlanetId = targetPlanetId });
		}
	}

	private void Update()
	{
		this.transform.position += (currentTarget - transform.position).normalized * shipSpeed * Time.deltaTime * 5;
	}
}
