using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuButton : CoButton
{
	[SerializeField] private TMP_Text buttonText;
	[SerializeField] private Image buttonImage;

	[SerializeField] private Color hightlightColor;
	[SerializeField] private Sprite highlightSprite;

	private Color defaultColor;
	private Sprite defaultSprite;

	void Start()
	{
		defaultColor = buttonText.color;
		defaultSprite = buttonImage.sprite;
	}

	public override void OnClick() { }

	public override void OnHoverEnd()
	{
		buttonImage.sprite = defaultSprite;
		buttonText.color = defaultColor;
	}

	public override void OnHoverStart()
	{
		buttonImage.sprite = highlightSprite;
		buttonText.color = hightlightColor;
	}
}
