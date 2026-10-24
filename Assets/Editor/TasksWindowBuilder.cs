using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the tasks and achievements windows and their HUD buttons in GameScene from the existing UI:
/// the window is a copy of the research window, rows use the mining panel background,
/// the claim button is a copy of ResearchButton and the HUD button a copy of a nav bar button.
/// Running it again replaces what it built before.
/// </summary>
public static class TasksWindowBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string WindowName = "TasksView";
	private const string HudButtonName = "TasksButton";
	private const string AchievementsWindowName = "AchievementsView";
	private const string AchievementsButtonName = "AchievementsButton";
	private const string LegacyPanelName = "TaskPanel";

	private const string WindowTemplatePath = "ResearchView";
	internal const string ClaimButtonTemplatePath = "ResearchInfoView/Panel/ResearchButton";
	internal const string BadgeTemplatePath = "ResearchInfoView/Panel/CloseButton";
	internal const string RowBackgroundTemplatePath = "PlanetInfoView/Panel/MiningInfoPanel";
	internal const string TextTemplatePath = "CurrenciesView/CurrencyField/CurrencyAmountText";
	internal const string SliderTemplateRoot = "ResourcesView";
	internal const string SliderTemplateName = "SellAmountSlider";

	private const float RowHeight = 220f;
	internal const float RowPadding = 32f;
	internal static readonly Color RewardColor = new Color(0.6f, 1f, 0.6f, 1f);

	[MenuItem("Tools/UI/Build Tasks And Achievements Windows")]
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

		foreach (string name in new[] { WindowName, HudButtonName, AchievementsWindowName, AchievementsButtonName, LegacyPanelName })
		{
			Transform old = canvas.Find(name);
			if (old != null)
			{
				Object.DestroyImmediate(old.gameObject);
			}
		}

		TMP_Text textTemplate = canvas.Find(TextTemplatePath).GetComponent<TMP_Text>();

		BuildWindow(canvas, textTemplate, MenuType.Tasks, WindowName, "Tasks", "All tasks completed");
		BuildHudButton(canvas, MenuType.Tasks, HudButtonName, "Tasks", -280);
		BuildWindow(canvas, textTemplate, MenuType.Achievements, AchievementsWindowName, "Achievements", "No achievements");
		BuildHudButton(canvas, MenuType.Achievements, AchievementsButtonName, "Goals", -400);
	}

	private static void BuildWindow(Transform canvas, TMP_Text textTemplate, MenuType type, string windowName, string title, string emptyText)
	{
		Transform panel;
		Button closeButton;
		ScrollRect scroll;
		GameObject window = CreateListWindow(canvas, windowName, title, out panel, out closeButton, out scroll);
		RectTransform content = scroll.content;

		TaskItem prototype = BuildRow(canvas, content, textTemplate);

		TMP_Text allCompletedText = CreateText("AllCompletedText", scroll.transform, textTemplate, 40, Color.white, TextAlignmentOptions.Center);
		Stretch(allCompletedText.rectTransform);
		allCompletedText.text = emptyText;
		allCompletedText.gameObject.SetActive(false);

		TasksView view = window.AddComponent<TasksView>();
		SerializedObject so = new SerializedObject(view);
		so.FindProperty("panel").objectReferenceValue = panel.gameObject;
		so.FindProperty("closeButton").objectReferenceValue = closeButton;
		so.FindProperty("itemContainer").objectReferenceValue = content;
		so.FindProperty("itemPrototype").objectReferenceValue = prototype;
		so.FindProperty("allCompletedText").objectReferenceValue = allCompletedText;
		so.FindProperty("type").enumValueIndex = (int)type;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	/// <summary>Copies the research window: frame, title bar, close button and a vertical list in its scroll area.</summary>
	internal static GameObject CreateListWindow(Transform canvas, string windowName, string title, out Transform panel, out Button closeButton, out ScrollRect scroll)
	{
		GameObject window = Object.Instantiate(canvas.Find(WindowTemplatePath).gameObject, canvas, false);
		window.name = windowName;
		window.transform.SetSiblingIndex(canvas.GetComponentInChildren<NavBarView>(true).transform.GetSiblingIndex());
		Object.DestroyImmediate(window.GetComponent<ResearchView>());

		panel = window.transform.Find("Panel");
		panel.gameObject.SetActive(false);
		panel.Find("TitleBG/ViewTitle").GetComponent<TMP_Text>().text = title;
		closeButton = panel.Find("TitleBG/CloseButton").GetComponent<Button>();
		closeButton.onClick = new Button.ButtonClickedEvent();

		scroll = panel.GetComponentInChildren<ScrollRect>(true);
		scroll.name = "ListLayout";
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
		return window;
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

	private static void BuildHudButton(Transform canvas, MenuType type, string name, string label, float y)
	{
		// Same look as the nav bar buttons, on the right edge below the top-right planet
		Transform navButton = canvas.GetComponentInChildren<NavBarView>(true).GetComponentInChildren<Button>(true).transform;
		Button button = CloneButton(navButton, canvas, name, label);
		button.transform.SetSiblingIndex(canvas.Find("CurrenciesView").GetSiblingIndex() + 1);
		foreach (LayoutElement element in button.GetComponents<LayoutElement>())
		{
			Object.DestroyImmediate(element);
		}

		SetRect((RectTransform)button.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, y), new Vector2(160, 100));

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
		so.FindProperty("type").enumValueIndex = (int)type;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	internal static TMP_Text CreateText(string name, Transform parent, TMP_Text template, float size, Color color, TextAlignmentOptions alignment)
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

	internal static Button CloneButton(Transform template, Transform parent, string name, string label)
	{
		GameObject go = Object.Instantiate(template.gameObject, parent, false);
		go.name = name;
		go.SetActive(true);

		Button button = go.GetComponent<Button>();
		button.onClick = new Button.ButtonClickedEvent();
		go.GetComponentInChildren<TMP_Text>(true).text = label;
		return button;
	}

	internal static Slider CloneSlider(Transform template, Transform parent)
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

	internal static Transform FindDeep(Transform root, string name)
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

	internal static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	internal static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
	{
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.pivot = pivot;
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}
}
