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

	/// <summary>Uses the given rock art at the given world size, with the hit area matching it.</summary>
	public void SetArt(Sprite sprite, float size)
	{
		if (sprite == null)
		{
			return;
		}

		GetComponent<SpriteRenderer>().sprite = sprite;
		float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
		transform.localScale = Vector3.one * (size / spriteSize);

		CircleCollider2D hitArea = GetComponent<CircleCollider2D>();
		if (hitArea != null)
		{
			hitArea.offset = Vector2.zero;
			hitArea.radius = spriteSize * 0.45f;
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.tag == "Missile")
		{
			signalBus.Fire(new AsteroidDestroyedSignal() { AsteroidId = id, Position = transform.position });
			Destroy(this.gameObject);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (PointerUtility.IsDrag(eventData))
		{
			return;
		}

		OnClick?.Invoke(transform.position);
	}

	private void Update()
	{
		this.transform.Rotate(new Vector3(0, 0, 1) * Time.deltaTime * 5);
	}
}
