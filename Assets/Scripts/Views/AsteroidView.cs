using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Zenject;

public class AsteroidView : MonoBehaviour, IPointerClickHandler
{
	private SignalBus signalBus;
	private int id;
	public UnityEvent<Vector3> OnClick;

	public void Init(SignalBus signalBus, int id)
	{
		this.signalBus = signalBus;
		this.id = id;
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.tag == "Missile")
		{
			signalBus.Fire(new AsteroidDestroyedSignal() { AsteroidId = id });
			Destroy(this.gameObject);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		OnClick?.Invoke(transform.position);
	}

	private void Update()
	{
		this.transform.Rotate(new Vector3(0, 0, 1) * Time.deltaTime * 5);
	}
}
