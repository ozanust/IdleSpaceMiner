using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.EventSystems;

public class PlanetSpaceView : MonoBehaviour, IPointerClickHandler
{
	[SerializeField] private SpriteRenderer planetSpriteRenderer;
	[SerializeField] private TMP_Text priceText;

	public UnityEvent<int> OnClick;
	private int index;

	/// <summary>Planets stay hidden until a telescope reveals them.</summary>
	public void Hide()
	{
		planetSpriteRenderer.enabled = false;
		priceText.gameObject.SetActive(false);
	}

    public void Unravel()
	{
		planetSpriteRenderer.enabled = true;
		priceText.gameObject.SetActive(true);
	}

	public void Unlock()
	{
		priceText.gameObject.SetActive(false);
		planetSpriteRenderer.color = Color.white;
	}

	public void SetPrice(long price)
	{
		priceText.text = NumberFormatter.FormatMoney(price);
	}

	public void SetPlanetIcon(Sprite icon)
	{
		planetSpriteRenderer.sprite = icon;
	}

	public void SetIndex(int index)
	{
		this.index = index;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (PointerUtility.IsDrag(eventData))
		{
			return;
		}

		OnClick?.Invoke(index);
	}
}
