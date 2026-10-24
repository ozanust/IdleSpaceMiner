using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the tasks window and its HUD button in GameScene from the existing UI:
/// the window is a copy of the research window, rows use the mining panel background,
/// the claim button is a copy of ResearchButton and the HUD button a copy of a nav bar button.
/// Running it again replaces what it built before.
/// </summary>
public static class TasksWindowBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string WindowName = "TasksView";
	private const string HudButtonName = "TasksButton";
	private const string LegacyPanelName = "TaskPanel";

	private const string WindowTemplatePath = "ResearchView";
	private const string ClaimButtonTemplatePath = "ResearchInfoView/Panel/ResearchButton";
	private const string BadgeTemplatePath = "ResearchInfoView/Panel/CloseButton";
	private const string RowBackgroundTemplatePath = "PlanetInfoView/Panel/MiningInfoPanel";
	private const string TextTemplatePath = "CurrenciesView/CurrencyField/CurrencyAmountText";
	private const string SliderTemplateRoot = "ResourcesView";
	private const string SliderTemplateName = "SellAmountSlider";

	private const float RowHeight = 220f;
	private const float RowPadding = 32f;
	private static readonly Color RewardColor = new Color(0.6f, 1f, 0.6f, 1f);

	[MenuItem("Tools/UI/Build Tasks Window")]
	public static void BuildInOpenScene()
	{
		Build();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: opens GameScene, builds the window and saves the scene.</summary>
	public static void BuildAndSaveScene()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Build();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Build()
	{
		Transform canvas = Object.FindFirstObjectByType<NavBarView>(FindObjectsInactive.Include).transform.parent;

		foreach (string name in new[] { WindowName, HudButtonName, LegacyPanelName })
		{
			Transform old = canvas.Find(name);
			if (old != null)
			{
				Object.DestroyImmediate(old.gameObject);
			}
		}

		TMP_Text textTemplate = canvas.Find(TextTemplatePath).GetComponent<TMP_Text>();

		BuildWindow(canvas, textTemplate);
		BuildHudButton(canvas);
	}

	private static void BuildWindow(Transform canvas, TMP_Text textTemplate)
	{
		// Same frame, title bar, close button and scroll area as the research window
		GameObject window = Object.Instantiate(canvas.Find(WindowTemplatePath).gameObject, canvas, false);
		window.name = WindowName;
		window.transform.SetSiblingIndex(canvas.GetComponentInChildren<NavBarView>(true).transform.GetSiblingIndex());
		Object.DestroyImmediate(window.GetComponent<ResearchView>());

		Transform panel = window.transform.Find("Panel");
		panel.gameObject.SetActive(false);
		panel.Find("TitleBG/ViewTitle").GetComponent<TMP_Text>().text = "Tasks";
		Button closeButton = panel.Find("TitleBG/CloseButton").GetComponent<Button>();
		closeButton.onClick = new Button.ButtonClickedEvent();

		ScrollRect scroll = panel.GetComponentInChildren<ScrollRect>(true);
		scroll.name = "TasksLayout";
		scroll.horizontal = false;
		if (scroll.horizontalScrollbar != null)
		{
			Object.DestroyImmediate(scroll.horizontalScrollbar.gameObject);
			scroll.horizontalScrollbar = null;
		}

		RectTransform content = scroll.content;
		for (int i = content.childCount - 1; i >= 0; i--)
		{
			Object.DestroyImmediate(content.GetChild(i).gameObject);
		}

		foreach (LayoutGroup group in content.GetComponents<LayoutGroup>())
		{
			Object.DestroyImmediate(group);
		}

		// Stretch content to the viewport width and grow downwards with the rows
		content.anchorMin = new Vector2(0, 1);
		content.anchorMax = new Vector2(1, 1);
		content.pivot = new Vector2(0.5f, 1);
		content.anchoredPosition = Vector2.zero;
		content.sizeDelta = Vector2.zero;

		VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(24, 24, 24, 24);
		layout.spacing = 20;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;

		ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
		if (fitter == null)
		{
			fitter = content.gameObject.AddComponent<ContentSizeFitter>();
		}

		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		TaskItem prototype = BuildRow(canvas, content, textTemplate);

		TMP_Text allCompletedText = CreateText("AllCompletedText", scroll.transform, textTemplate, 40, Color.white, TextAlignmentOptions.Center);
		Stretch(allCompletedText.rectTransform);
		allCompletedText.text = "All tasks completed";
		allCompletedText.gameObject.SetActive(false);

		TasksView view = window.AddComponent<TasksView>();
		SerializedObject so = new SerializedObject(view);
		so.FindProperty("panel").objectReferenceValue = panel.gameObject;
		so.FindProperty("closeButton").objectReferenceValue = closeButton;
		so.FindProperty("itemContainer").objectReferenceValue = content;
		so.FindProperty("itemPrototype").objectReferenceValue = prototype;
		so.FindProperty("allCompletedText").objectReferenceValue = allCompletedText;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static TaskItem BuildRow(Transform canvas, Transform content, TMP_Text textTemplate)
	{
		Image backgroundTemplate = canvas.Find(RowBackgroundTemplatePath).GetComponent<Image>();

		GameObject row = new GameObject("TaskItem", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
		row.layer = canvas.gameObject.layer;
		RectTransform rowRect = (RectTransform)row.transform;
		rowRect.SetParent(content, false);

		Image background = row.GetComponent<Image>();
		background.sprite = backgroundTemplate.sprite;
		background.type = backgroundTemplate.type;
		background.color = backgroundTemplate.color;
		row.GetComponent<LayoutElement>().preferredHeight = RowHeight;

		const float claimWidth = 240f;

		TMP_Text descriptionText = CreateText("DescriptionText", rowRect, textTemplate, 40, Color.white, TextAlignmentOptions.Left);
		SetRect(descriptionText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(RowPadding, -20), new Vector2(-RowPadding * 2, 52));
		descriptionText.enableAutoSizing = true;
		descriptionText.fontSizeMin = 26;
		descriptionText.fontSizeMax = 40;
		descriptionText.text = "Collect 50 Iron";

		TMP_Text rewardText = CreateText("RewardText", rowRect, textTemplate, 34, RewardColor, TextAlignmentOptions.Left);
		SetRect(rewardText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(RowPadding, -76), new Vector2(-(RowPadding * 3 + claimWidth), 44));
		rewardText.text = "Reward: $150";

		Slider slider = CloneSlider(FindDeep(canvas.Find(SliderTemplateRoot), SliderTemplateName), rowRect);
		RectTransform sliderRect = (RectTransform)slider.transform;
		sliderRect.anchorMin = new Vector2(0, 0);
		sliderRect.anchorMax = new Vector2(1, 0);
		sliderRect.pivot = new Vector2(0.5f, 0);
		sliderRect.offsetMin = new Vector2(RowPadding, 66);
		sliderRect.offsetMax = new Vector2(-(RowPadding * 3 + claimWidth), 84);

		TMP_Text progressText = CreateText("ProgressText", rowRect, textTemplate, 30, Color.white, TextAlignmentOptions.Left);
		SetRect(progressText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(RowPadding, 16), new Vector2(400, 44));
		progressText.text = "24 / 50";

		Button claimButton = CloneButton(canvas.Find(ClaimButtonTemplatePath), rowRect, "ClaimButton", "Claim");
		SetRect((RectTransform)claimButton.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-RowPadding, -20), new Vector2(claimWidth, 90));

		TaskItem item = row.AddComponent<TaskItem>();
		SerializedObject so = new SerializedObject(item);
		so.FindProperty("descriptionText").objectReferenceValue = descriptionText;
		so.FindProperty("rewardText").objectReferenceValue = rewardText;
		so.FindProperty("progressText").objectReferenceValue = progressText;
		so.FindProperty("progressSlider").objectReferenceValue = slider;
		so.FindProperty("claimButton").objectReferenceValue = claimButton;
		so.ApplyModifiedPropertiesWithoutUndo();

		return item;
	}

	private static void BuildHudButton(Transform canvas)
	{
		// Same look as the nav bar buttons, on the right edge below the top-right planet
		Transform navButton = canvas.GetComponentInChildren<NavBarView>(true).GetComponentInChildren<Button>(true).transform;
		Button button = CloneButton(navButton, canvas, HudButtonName, "Tasks");
		button.transform.SetSiblingIndex(canvas.Find("CurrenciesView").GetSiblingIndex() + 1);
		foreach (LayoutElement element in button.GetComponents<LayoutElement>())
		{
			Object.DestroyImmediate(element);
		}

		SetRect((RectTransform)button.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -280), new Vector2(160, 100));

		Image badgeTemplate = canvas.Find(BadgeTemplatePath).GetComponent<Image>();
		GameObject badge = new GameObject("ClaimableBadge", typeof(RectTransform), typeof(Image));
		badge.layer = canvas.gameObject.layer;
		RectTransform badgeRect = (RectTransform)badge.transform;
		badgeRect.SetParent(button.transform, false);
		SetRect(badgeRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-8, -8), new Vector2(36, 36));
		Image badgeImage = badge.GetComponent<Image>();
		badgeImage.sprite = badgeTemplate.sprite;
		badgeImage.color = new Color(0.2f, 0.9f, 0.4f, 1f);
		badgeImage.raycastTarget = false;
		badge.SetActive(false);

		TasksButton tasksButton = button.gameObject.AddComponent<TasksButton>();
		SerializedObject so = new SerializedObject(tasksButton);
		so.FindProperty("button").objectReferenceValue = button;
		so.FindProperty("claimableBadge").objectReferenceValue = badge;
		so.ApplyModifiedPropertiesWithoutUndo();
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
		return text;
	}

	private static Button CloneButton(Transform template, Transform parent, string name, string label)
	{
		GameObject go = Object.Instantiate(template.gameObject, parent, false);
		go.name = name;
		go.SetActive(true);

		Button button = go.GetComponent<Button>();
		button.onClick = new Button.ButtonClickedEvent();
		go.GetComponentInChildren<TMP_Text>(true).text = label;
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

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
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
