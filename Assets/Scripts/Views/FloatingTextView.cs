using TMPro;
using UnityEngine;

/// <summary>World space text that rises and fades out, e.g. the reward of a destroyed asteroid.</summary>
public class FloatingTextView : MonoBehaviour
{
	private const float Duration = 1.8f;
	private const float RiseDistance = 3f;
	private const float FontSize = 16f;

	public static void Spawn(string text, Vector3 position, Color color)
	{
		GameObject go = new GameObject("FloatingText", typeof(TextMeshPro), typeof(FloatingTextView));
		go.transform.position = position;

		TextMeshPro tmp = go.GetComponent<TextMeshPro>();
		tmp.text = text;
		tmp.fontSize = FontSize;
		tmp.fontStyle = FontStyles.Bold;
		tmp.color = color;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.textWrappingMode = TextWrappingModes.NoWrap;
		tmp.rectTransform.sizeDelta = new Vector2(20f, 3f);
		// Above planets and the mothership
		tmp.sortingOrder = 10;

		LeanTween.moveY(go, position.y + RiseDistance, Duration).setEase(LeanTweenType.easeOutCubic);
		LeanTween.value(go, 1f, 0f, Duration * 0.5f)
			.setDelay(Duration * 0.5f)
			.setOnUpdate((float alpha) => tmp.alpha = alpha)
			.setOnComplete(() => Destroy(go));
	}
}
