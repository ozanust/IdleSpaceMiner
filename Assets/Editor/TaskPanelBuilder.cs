using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the HUD task panel in GameScene, styled after the existing panels
/// (Background sprite, LiberationSans texts, ResearchButton and sell slider copies).
/// Running it again replaces the previous panel.
/// </summary>
public static class TaskPanelBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string PanelName = "TaskPanel";

	private const string ButtonTemplatePath = "ResearchInfoView/Panel/ResearchButton";
	private const string SliderTemplateRoot = "ResourcesView";
	private const string SliderTemplateName = "SellAmountSlider";
	private const string BackgroundTemplatePath = "ResourcesView/Panel/TitleBG";
	private const string TextTemplatePath = "CurrenciesView/CurrencyField/CurrencyAmountText";

	private const float Padding = 32f;

	[MenuItem("Tools/UI/Build Task Panel")]
	public static void BuildInOpenScene()
	{
		Build();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: opens GameScene, builds the panel and saves the scene.</summary>
	public static void BuildAndSaveScene()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Build();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Build()
	{
		Transform canvas = Object.FindFirstObjectByType<NavBarView>(FindObjectsInactive.Include).transform.parent;

		Transform old = canvas.Find(PanelName);
		if (old != null)
		{
			Object.DestroyImmediate(old.gameObject);
		}

		Image backgroundTemplate = canvas.Find(BackgroundTemplatePath).GetComponent<Image>();
		TMP_Text textTemplate = canvas.Find(TextTemplatePath).GetComponent<TMP_Text>();

		// Panel root, top centre of the HUD, between the currencies and the top-right planet
		GameObject panel = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
		panel.layer = canvas.gameObject.layer;
		RectTransform panelRect = (RectTransform)panel.transform;
		panelRect.SetParent(canvas, false);
		panelRect.SetSiblingIndex(canvas.Find("CurrenciesView").GetSiblingIndex() + 1);
		SetRect(panelRect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-10, -70), new Vector2(520, 200));

		Image background = panel.GetComponent<Image>();
		background.sprite = backgroundTemplate.sprite;
		background.type = backgroundTemplate.type;
		background.color = backgroundTemplate.color;

		TMP_Text counterText = CreateText("TaskCounterText", panelRect, textTemplate, 24, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.Left);
		SetRect(counterText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(Padding, -16), new Vector2(300, 36));

		TMP_Text rewardText = CreateText("RewardText", panelRect, textTemplate, 28, new Color(0.6f, 1f, 0.6f, 1f), TextAlignmentOptions.Right);
		SetRect(rewardText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-Padding, -16), new Vector2(240, 36));
		rewardText.text = "$150";

		TMP_Text descriptionText = CreateText("DescriptionText", panelRect, textTemplate, 34, Color.white, TextAlignmentOptions.Left);
		SetRect(descriptionText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -54), new Vector2(-Padding * 2, 48));
		descriptionText.enableAutoSizing = true;
		descriptionText.fontSizeMin = 22;
		descriptionText.fontSizeMax = 34;
		descriptionText.text = "Collect 50 Iron";

		Button claimButton = CloneButton(canvas.Find(ButtonTemplatePath), panelRect);
		SetRect((RectTransform)claimButton.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-Padding, 18), new Vector2(170, 66));

		Slider progressSlider = CloneSlider(FindDeep(canvas.Find(SliderTemplateRoot), SliderTemplateName), panelRect);
		RectTransform sliderRect = (RectTransform)progressSlider.transform;
		sliderRect.anchorMin = new Vector2(0, 0);
		sliderRect.anchorMax = new Vector2(1, 0);
		sliderRect.pivot = new Vector2(0.5f, 0);
		sliderRect.offsetMin = new Vector2(Padding, 64);
		sliderRect.offsetMax = new Vector2(-(Padding * 2 + 170), 80);

		TMP_Text progressText = CreateText("ProgressText", panelRect, textTemplate, 28, Color.white, TextAlignmentOptions.Left);
		SetRect(progressText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(Padding, 18), new Vector2(300, 40));
		progressText.text = "24 / 50";

		TaskView taskView = panel.AddComponent<TaskView>();
		SerializedObject so = new SerializedObject(taskView);
		so.FindProperty("descriptionText").objectReferenceValue = descriptionText;
		so.FindProperty("progressText").objectReferenceValue = progressText;
		so.FindProperty("rewardText").objectReferenceValue = rewardText;
		so.FindProperty("claimButton").objectReferenceValue = claimButton;
		so.FindProperty("progressSlider").objectReferenceValue = progressSlider;
		so.FindProperty("taskCounterText").objectReferenceValue = counterText;
		so.ApplyModifiedPropertiesWithoutUndo();

		Selection.activeGameObject = panel;
	}

	private static TMP_Text CreateText(string name, Transform parent, TMP_Text template, float size, Color color, TextAlignmentOptions alignment)
	{
		GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
		go.layer = parent.gameObject.layer;
		go.transform.SetParent(parent, false);

		TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
		text.font = template.font;
		text.fontSharedMaterial = template.fontSharedMaterial;
		text.fontSize = size;
		text.color = color;
		text.alignment = alignment;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Ellipsis;
		text.raycastTarget = false;
		text.text = "Task 2 / 19";
		return text;
	}

	private static Button CloneButton(Transform template, Transform parent)
	{
		GameObject go = Object.Instantiate(template.gameObject, parent, false);
		go.name = "ClaimButton";
		go.SetActive(true);

		Button button = go.GetComponent<Button>();
		button.onClick = new Button.ButtonClickedEvent();

		TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
		label.text = "Claim";
		return button;
	}

	private static Slider CloneSlider(Transform template, Transform parent)
	{
		GameObject go = Object.Instantiate(template.gameObject, parent, false);
		go.name = "ProgressSlider";
		go.SetActive(true);

		Slider slider = go.GetComponent<Slider>();
		slider.onValueChanged = new Slider.SliderEvent();
		slider.interactable = false;
		slider.transition = Selectable.Transition.None;
		slider.wholeNumbers = false;
		slider.minValue = 0;
		slider.maxValue = 1;
		slider.value = 0.48f;

		// Display-only bar, so drop the drag handle and the sell slider's value labels
		Transform handleArea = go.transform.Find("Handle Slide Area");
		if (handleArea != null)
		{
			slider.handleRect = null;
			Object.DestroyImmediate(handleArea.gameObject);
		}

		foreach (TMP_Text label in go.GetComponentsInChildren<TMP_Text>(true))
		{
			Object.DestroyImmediate(label.gameObject);
		}

		return slider;
	}

	private static Transform FindDeep(Transform root, string name)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			if (child.name == name)
			{
				return child;
			}
		}

		return null;
	}

	private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
	{
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.pivot = pivot;
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}
}
