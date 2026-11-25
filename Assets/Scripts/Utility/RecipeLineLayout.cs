using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays out a recipe line "ingredient(s) -> result" centred, with tighter spacing and smaller
/// icons the more ingredients there are, so up to three fit on a card.
/// </summary>
public static class RecipeLineLayout
{
	// Indexed by ingredient count - 1
	private static readonly float[] Spacing = { 110f, 84f, 68f };
	private static readonly float[] IconSize = { 92f, 72f, 58f };

	/// <summary>Shows the first count ingredient images (hiding the rest) and places everything.</summary>
	public static void Layout(Image[] ingredients, int count, RectTransform arrow, RectTransform result)
	{
		count = Mathf.Clamp(count, 1, ingredients.Length);
		float spacing = Spacing[count - 1];
		float size = IconSize[count - 1];

		// count ingredients + arrow + result, centred around 0
		float first = -(count + 1) / 2f;
		for (int i = 0; i < ingredients.Length; i++)
		{
			if (ingredients[i] == null)
			{
				continue;
			}

			bool visible = i < count;
			ingredients[i].gameObject.SetActive(visible);
			if (visible)
			{
				Place(ingredients[i].rectTransform, (first + i) * spacing, size);
			}
		}

		if (arrow != null)
		{
			Place(arrow, (first + count) * spacing, 0);
		}

		Place(result, (first + count + 1) * spacing, size);
	}

	private static void Place(RectTransform rect, float x, float size)
	{
		rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
		if (size > 0)
		{
			rect.sizeDelta = new Vector2(size, size);
		}
	}
}
