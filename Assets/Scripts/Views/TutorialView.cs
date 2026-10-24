using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

/// <summary>
/// Tutorial overlay: a hint box, a pulsing ring around what to tap and an arrow pointing at it.
/// Never blocks taps on the game, only its own buttons take input.
/// </summary>
public class TutorialView : MonoBehaviour
{
	private const float TargetRefreshInterval = 0.2f;
	private const float EdgeMargin = 70f;

	[Inject] ITutorialController tutorialController;

	[SerializeField] private GameObject root;
	[SerializeField] private RectTransform hintPanel;
	[SerializeField] private TMP_Text hintText;
	[SerializeField] private Button continueButton;
	[SerializeField] private Button skipButton;
	[SerializeField] private RectTransform ring;
	[SerializeField] private RectTransform pointer;

	[SerializeField] private float hintMarginTop = 30f;
	[SerializeField] private float hintMarginBottom = 170f;
	[SerializeField] private float pointerDistance = 70f;

	private RectTransform rect;
	private Canvas canvas;
	private TutorialStepInfo shownStep;
	private TutorialTarget target;
	private float nextTargetRefresh;

	private void Awake()
	{
		rect = (RectTransform)transform;
		canvas = GetComponentInParent<Canvas>().rootCanvas;
		continueButton.onClick.AddListener(() => tutorialController.Continue());
		skipButton.onClick.AddListener(() => tutorialController.Skip());
		root.SetActive(false);
	}

	private void LateUpdate()
	{
		TutorialStepInfo step = tutorialController.CurrentStep;
		root.SetActive(step != null);
		if (step == null)
		{
			shownStep = null;
			return;
		}

		if (step != shownStep)
		{
			shownStep = step;
			hintText.text = step.Text;
			continueButton.gameObject.SetActive(step.HasContinue);
			nextTargetRefresh = 0f;
		}

		if (Time.unscaledTime >= nextTargetRefresh)
		{
			nextTargetRefresh = Time.unscaledTime + TargetRefreshInterval;
			target = FindTarget(step);
		}

		Vector2 position = Vector2.zero;
		float radius = 0f;
		bool hasTarget = target != null && TryGetTargetOnCanvas(target, out position, out radius);
		ring.gameObject.SetActive(hasTarget);
		pointer.gameObject.SetActive(hasTarget);

		if (!hasTarget)
		{
			PlaceHint(true);
			return;
		}

		PlaceHint(position.y < 0f);
		Animate(position, radius);
	}

	private static TutorialTarget FindTarget(TutorialStepInfo step)
	{
		foreach (TutorialTargetId id in step.Targets)
		{
			TutorialTarget found = TutorialTarget.FindVisible(id);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	/// <summary>Target centre in this overlay's local space and its rough radius, clamped onto the screen.</summary>
	private bool TryGetTargetOnCanvas(TutorialTarget tutorialTarget, out Vector2 position, out float radius)
	{
		Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
		Vector3 min;
		Vector3 max;

		RectTransform targetRect = tutorialTarget.transform as RectTransform;
		if (targetRect != null)
		{
			Vector3[] corners = new Vector3[4];
			targetRect.GetWorldCorners(corners);
			min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
			max = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
		}
		else
		{
			Camera worldCamera = Camera.main;
			// Planets also hold a price text renderer, the sprite gives the real size
			Renderer renderer = tutorialTarget.GetComponentInChildren<SpriteRenderer>();
			if (renderer == null)
			{
				renderer = tutorialTarget.GetComponentInChildren<Renderer>();
			}

			if (worldCamera == null || renderer == null)
			{
				position = Vector2.zero;
				radius = 0f;
				return false;
			}

			min = worldCamera.WorldToScreenPoint(renderer.bounds.min);
			max = worldCamera.WorldToScreenPoint(renderer.bounds.max);
		}

		Vector2 localMin;
		Vector2 localMax;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, min, uiCamera, out localMin);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, max, uiCamera, out localMax);

		position = (localMin + localMax) * 0.5f;
		radius = Mathf.Max(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y)) * 0.5f;

		// Keep pointing from the screen edge when the camera is panned away from the target
		Rect bounds = rect.rect;
		position.x = Mathf.Clamp(position.x, bounds.xMin + EdgeMargin, bounds.xMax - EdgeMargin);
		position.y = Mathf.Clamp(position.y, bounds.yMin + EdgeMargin, bounds.yMax - EdgeMargin);
		return true;
	}

	private void Animate(Vector2 position, float radius)
	{
		float time = Time.unscaledTime;

		ring.anchoredPosition = position;
		float ringSize = radius * 2f + 50f;
		ring.sizeDelta = new Vector2(ringSize, ringSize);
		ring.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(time * 5f));

		// The arrow sits on the screen centre side of the target and points at it
		Vector2 away = -position;
		away = away.sqrMagnitude > 1f ? away.normalized : Vector2.up;
		float bob = 14f * Mathf.Sin(time * 6f);
		pointer.anchoredPosition = position + away * (radius + pointerDistance + bob);
		pointer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-away.y, -away.x) * Mathf.Rad2Deg);
	}

	// Hint sits at the top or bottom of the screen, away from the target
	private void PlaceHint(bool top)
	{
		float y = top ? 1f : 0f;
		hintPanel.anchorMin = new Vector2(0.5f, y);
		hintPanel.anchorMax = new Vector2(0.5f, y);
		hintPanel.pivot = new Vector2(0.5f, y);
		hintPanel.anchoredPosition = new Vector2(0f, top ? -hintMarginTop : hintMarginBottom);
	}
}
