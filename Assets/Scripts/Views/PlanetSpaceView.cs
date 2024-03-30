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

	public void SetPrice(int price)
	{
		priceText.text = string.Format("{0}{1}", "$", price.ToString());
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
		OnClick?.Invoke(index);
	}
}
