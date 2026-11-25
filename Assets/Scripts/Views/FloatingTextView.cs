using TMPro;
using UnityEngine;

/// <summary>World space text, optionally with an icon, that rises and fades out, e.g. the reward of a destroyed asteroid.</summary>
public class FloatingTextView : MonoBehaviour
{
	private const float Duration = 1.8f;
	private const float RiseDistance = 3f;
	private const float DefaultFontSize = 16f;
	private const float OutlineWidth = 0.25f;
	private static readonly Color32 OutlineColor = new Color32(8, 16, 24, 255);

	public static void Spawn(string text, Vector3 position, Color color, float fontSize = DefaultFontSize)
	{
		GameObject go = new GameObject("FloatingText", typeof(TextMeshPro), typeof(FloatingTextView));
		go.transform.position = position;

		TextMeshPro tmp = go.GetComponent<TextMeshPro>();
		tmp.text = text;
		tmp.fontSize = fontSize;
		tmp.fontStyle = FontStyles.Bold;
		tmp.color = color;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.textWrappingMode = TextWrappingModes.NoWrap;
		tmp.rectTransform.sizeDelta = new Vector2(20f, 3f);
		// Above planets and the mothership
		tmp.sortingOrder = 10;

		Animate(go, position, alpha => tmp.alpha = alpha);
	}

	/// <summary>An icon followed by a text, e.g. a resource icon and "+12", centred on the position.</summary>
	public static void SpawnWithIcon(Sprite icon, string text, Vector3 position, Color color, float fontSize, float iconSize)
	{
		const float gap = 0.15f;

		GameObject go = new GameObject("FloatingIconText", typeof(FloatingTextView));
		go.transform.position = position;

		GameObject textObject = new GameObject("Text", typeof(TextMeshPro));
		textObject.transform.SetParent(go.transform, false);
		TextMeshPro tmp = textObject.GetComponent<TextMeshPro>();
		tmp.text = text;
		tmp.fontSize = fontSize;
		tmp.fontStyle = FontStyles.Bold;
		tmp.color = color;
		tmp.alignment = TextAlignmentOptions.Left;
		tmp.textWrappingMode = TextWrappingModes.NoWrap;
		tmp.sortingOrder = 10;
		// A dark outline keeps the amount readable over both bright and dark parts of the nebula
		tmp.outlineWidth = OutlineWidth;
		tmp.outlineColor = OutlineColor;

		// Centre icon and text together as one row
		float textWidth = tmp.GetPreferredValues(text).x;
		float left = -(iconSize + gap + textWidth) * 0.5f;
		tmp.rectTransform.pivot = new Vector2(0f, 0.5f);
		tmp.rectTransform.sizeDelta = new Vector2(textWidth + 0.5f, iconSize * 1.5f);
		tmp.rectTransform.localPosition = new Vector3(left + iconSize + gap, 0f, 0f);

		SpriteRenderer iconRenderer = null;
		if (icon != null)
		{
			GameObject iconObject = new GameObject("Icon", typeof(SpriteRenderer));
			iconObject.transform.SetParent(go.transform, false);
			iconRenderer = iconObject.GetComponent<SpriteRenderer>();
			iconRenderer.sprite = icon;
			iconRenderer.sortingOrder = 10;
			float spriteSize = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
			iconObject.transform.localScale = Vector3.one * (spriteSize > 0f ? iconSize / spriteSize : 1f);
			iconObject.transform.localPosition = new Vector3(left + iconSize * 0.5f, 0f, 0f);
		}

		Animate(go, position, alpha =>
		{
			tmp.alpha = alpha;
			if (iconRenderer != null)
			{
				Color iconColor = iconRenderer.color;
				iconColor.a = alpha;
				iconRenderer.color = iconColor;
			}
		});
	}

	// Rises, then fades out over the second half and removes itself
	private static void Animate(GameObject go, Vector3 position, System.Action<float> setAlpha)
	{
		LeanTween.moveY(go, position.y + RiseDistance, Duration).setEase(LeanTweenType.easeOutCubic);
		LeanTween.value(go, 1f, 0f, Duration * 0.5f)
			.setDelay(Duration * 0.5f)
			.setOnUpdate((float alpha) => setAlpha(alpha))
			.setOnComplete(() => Destroy(go));
	}
}
