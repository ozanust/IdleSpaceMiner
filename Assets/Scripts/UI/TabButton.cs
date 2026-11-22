using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A sub-category tab in a window, like Ores/Alloys/Items. The selected tab uses the bright
/// button art and a white label, the others the dark art with a dimmer label.
/// Sprites are filled in by Tools > UI > Apply Orbital Theme.
/// </summary>
[RequireComponent(typeof(Button))]
public class TabButton : MonoBehaviour
{
	[SerializeField] private Sprite normalSprite;
	[SerializeField] private SpriteState normalState;
	[SerializeField] private Sprite selectedSprite;
	[SerializeField] private SpriteState selectedState;
	[SerializeField] private Color normalLabelColor = new Color(0.62f, 0.78f, 0.88f, 1f);
	[SerializeField] private Color selectedLabelColor = Color.white;

	public void SetSprites(Sprite normal, SpriteState normalStates, Sprite selected, SpriteState selectedStates)
	{
		normalSprite = normal;
		normalState = normalStates;
		selectedSprite = selected;
		selectedState = selectedStates;
	}

	public void SetSelected(bool selected)
	{
		Button button = GetComponent<Button>();
		Image image = button.image;
		Sprite sprite = selected ? selectedSprite : normalSprite;
		if (image != null && sprite != null)
		{
			image.sprite = sprite;
			button.spriteState = selected ? selectedState : normalState;
		}

		TMP_Text label = GetComponentInChildren<TMP_Text>(true);
		if (label != null)
		{
			label.color = selected ? selectedLabelColor : normalLabelColor;
		}
	}

	/// <summary>Marks the tab at the given index as selected and the others as not.</summary>
	public static void Select(int index, params Button[] tabs)
	{
		for (int i = 0; i < tabs.Length; i++)
		{
			TabButton tab = tabs[i] != null ? tabs[i].GetComponent<TabButton>() : null;
			if (tab != null)
			{
				tab.SetSelected(i == index);
			}
		}
	}
}
