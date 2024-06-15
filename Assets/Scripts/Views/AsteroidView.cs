using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class AsteroidView : MonoBehaviour
{
	private SignalBus signalBus;
	private int id;

	public void Init(SignalBus signalBus, int id)
	{
		this.signalBus = signalBus;
		this.id = id;
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.tag == "Missile")
		{
			signalBus.Fire(new AsteroidDestroyedSignal() { AsteroidId = id });
		}
	}
}
