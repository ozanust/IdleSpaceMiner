using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stand-in for rewarded ads while the AdMob SDK isn't set up, and for testing in the editor:
/// covers the screen with a short "test ad" countdown, then grants the reward.
/// </summary>
public class SimulatedRewardedAdService : IRewardedAdService
{
	private const float AdSeconds = 2f;

	private bool showing;

	public bool IsReady => !showing;

	public void Show(Action<bool> onFinished)
	{
		if (showing)
		{
			onFinished?.Invoke(false);
			return;
		}

		showing = true;
		SimulatedAdOverlay.Show(AdSeconds, () =>
		{
			showing = false;
			onFinished?.Invoke(true);
		});
	}

	/// <summary>Full screen overlay that blocks input while the pretend ad plays.</summary>
	private class SimulatedAdOverlay : MonoBehaviour
	{
		public static void Show(float seconds, Action onDone)
		{
			GameObject root = new GameObject("SimulatedAd", typeof(Canvas), typeof(GraphicRaycaster));
			Canvas canvas = root.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 1000;

			GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
			background.transform.SetParent(root.transform, false);
			RectTransform backgroundRect = (RectTransform)background.transform;
			backgroundRect.anchorMin = Vector2.zero;
			backgroundRect.anchorMax = Vector2.one;
			backgroundRect.offsetMin = Vector2.zero;
			backgroundRect.offsetMax = Vector2.zero;
			background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

			GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
			label.transform.SetParent(background.transform, false);
			RectTransform labelRect = (RectTransform)label.transform;
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.offsetMin = Vector2.zero;
			labelRect.offsetMax = Vector2.zero;
			TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
			text.alignment = TextAlignmentOptions.Center;
			text.fontSize = 48;

			root.AddComponent<SimulatedAdOverlay>().StartCoroutine(Play(root, text, seconds, onDone));
		}

		private static IEnumerator Play(GameObject root, TextMeshProUGUI text, float seconds, Action onDone)
		{
			float remaining = seconds;
			while (remaining > 0f)
			{
				text.text = $"Test rewarded ad\n{Mathf.CeilToInt(remaining)}";
				remaining -= Time.unscaledDeltaTime;
				yield return null;
			}

			Destroy(root);
			onDone?.Invoke();
		}
	}
}
