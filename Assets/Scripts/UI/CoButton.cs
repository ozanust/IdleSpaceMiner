using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class CoButton : Selectable, IPointerClickHandler
{
	public event Action onClick;

	public override void OnPointerEnter(PointerEventData eventData)
	{
		OnHoverStart();
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		OnHoverEnd();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		OnClick();
		onClick?.Invoke();
	}

	public abstract void OnHoverStart();
	public abstract void OnHoverEnd();
	public abstract void OnClick();
}
