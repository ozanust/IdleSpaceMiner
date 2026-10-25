using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the ad boosts UI in GameScene: the Boosts window with the Mine Boost and Ark rows, and
/// the HUD with the Ark button and Mine Boost timer. Shares the tasks window helpers.
/// Running it again replaces what it built before.
/// </summary>
public static class BoostsBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string WindowName = "BoostsView";
	private const string HudName = "BoostHud";
	private const string ChestSpritePath = "Assets/Sci-Fi Game UI 8/MAIN/PNG/Treasure_ChestICON.png";

	private const float RowHeight = 250f;
	private const float ButtonWidth = 320f;

	private static readonly Color MutedColor = new Color(0.55f, 0.85f, 0.92f, 1f);
	private static readonly Color GoldColor = new Color(1f, 0.85f, 0.4f, 1f);

	[MenuItem("Tools/UI/Build Boosts")]
	public static void BuildInOpenScene()
	{
		Build();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: opens GameScene, builds and saves the scene.</summary>
	public static void BuildAndSaveScene()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Build();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Build()
	{
		Transform canvas = Object.FindAnyObjectByType<NavBarView>(FindObjectsInactive.Include).transform.parent;
		foreach (string name in new[] { WindowName, HudName })
		{
			Transform old = canvas.Find(name);
			if (old != null)
			{
				Object.DestroyImmediate(old.gameObject);
			}
		}

		TMP_Text textTemplate = canvas.Find(TasksWindowBuilder.TextTemplatePath).GetComponent<TMP_Text>();
		Transform buttonTemplate = canvas.Find(TasksWindowBuilder.ClaimButtonTemplatePath);

		BuildWindow(canvas, textTemplate, buttonTemplate);
		BuildHud(canvas, textTemplate);
	}

	private static void BuildWindow(Transform canvas, TMP_Text textTemplate, Transform buttonTemplate)
	{
		Transform panel;
		Button closeButton;
		ScrollRect scroll;
		GameObject window = TasksWindowBuilder.CreateListWindow(canvas, WindowName, "Boosts", out panel, out closeButton, out scroll);

		TMP_Text mineDescription;
		TMP_Text mineStatus;
		Button mineButton = BuildRow(canvas, scroll.content, textTemplate, buttonTemplate, "MineBoost", "Mine Boost",
			"Mining x1.2 on all planets for 60 minutes", "Watch ad", out mineDescription, out mineStatus);

		TMP_Text arkDescription;
		TMP_Text arkStatus;
		Button arkButton = BuildRow(canvas, scroll.content, textTemplate, buttonTemplate, "Ark", "Ark",
			"A gift every few minutes: Dark Matter or cash", "Watch ad to claim", out arkDescription, out arkStatus);

		BoostsView view = window.AddComponent<BoostsView>();
		SerializedObject so = new SerializedObject(view);
		so.FindProperty("panel").objectReferenceValue = panel.gameObject;
		so.FindProperty("closeButton").objectReferenceValue = closeButton;
		so.FindProperty("mineBoostDescriptionText").objectReferenceValue = mineDescription;
		so.FindProperty("mineBoostStatusText").objectReferenceValue = mineStatus;
		so.FindProperty("mineBoostButton").objectReferenceValue = mineButton;
		so.FindProperty("arkStatusText").objectReferenceValue = arkStatus;
		so.FindProperty("arkButton").objectReferenceValue = arkButton;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	/// <summary>One boost row: title, description and status on the left, the ad button on the right.</summary>
	private static Button BuildRow(Transform canvas, Transform content, TMP_Text textTemplate, Transform buttonTemplate, string name,
		string title, string description, string buttonLabel, out TMP_Text descriptionText, out TMP_Text statusText)
	{
		Image backgroundTemplate = canvas.Find(TasksWindowBuilder.RowBackgroundTemplatePath).GetComponent<Image>();
		float pad = TasksWindowBuilder.RowPadding;

		GameObject row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
		row.layer = canvas.gameObject.layer;
		RectTransform rowRect = (RectTransform)row.transform;
		rowRect.SetParent(content, false);
		Image background = row.GetComponent<Image>();
		background.sprite = backgroundTemplate.sprite;
		background.type = backgroundTemplate.type;
		background.color = backgroundTemplate.color;
		row.GetComponent<LayoutElement>().preferredHeight = RowHeight;

		float textWidth = -(pad * 3 + ButtonWidth);

		TMP_Text titleText = TasksWindowBuilder.CreateText("TitleText", rowRect, textTemplate, 42, Color.white, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(titleText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(pad, -22), new Vector2(textWidth, 56));
		titleText.fontStyle = FontStyles.Bold;
		titleText.text = title;

		descriptionText = TasksWindowBuilder.CreateText("DescriptionText", rowRect, textTemplate, 30, MutedColor, TextAlignmentOptions.TopLeft);
		TasksWindowBuilder.SetRect(descriptionText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(pad, -84), new Vector2(textWidth, 84));
		descriptionText.textWrappingMode = TextWrappingModes.Normal;
		descriptionText.text = description;

		statusText = TasksWindowBuilder.CreateText("StatusText", rowRect, textTemplate, 30, GoldColor, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(statusText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(pad, 22), new Vector2(textWidth, 44));
		statusText.text = "Not active";

		Button button = TasksWindowBuilder.CloneButton(buttonTemplate, rowRect, "AdButton", buttonLabel);
		TasksWindowBuilder.SetRect((RectTransform)button.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-pad, 0), new Vector2(ButtonWidth, 96));
		TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
		label.enableAutoSizing = true;
		label.fontSizeMin = 20;
		label.fontSizeMax = 34;
		return button;
	}

	private static void BuildHud(Transform canvas, TMP_Text textTemplate)
	{
		GameObject hud = new GameObject(HudName, typeof(RectTransform));
		hud.layer = canvas.gameObject.layer;
		RectTransform hudRect = (RectTransform)hud.transform;
		hudRect.SetParent(canvas, false);
		hudRect.SetSiblingIndex(canvas.Find("CurrenciesView").GetSiblingIndex() + 1);
		TasksWindowBuilder.Stretch(hudRect);

		// Same look as the right edge HUD buttons, under the currencies on the left
		Transform navButton = canvas.GetComponentInChildren<NavBarView>(true).GetComponentInChildren<Button>(true).transform;
		Button arkButton = TasksWindowBuilder.CloneButton(navButton, hudRect, "ArkButton", "Ark");
		foreach (LayoutElement element in arkButton.GetComponents<LayoutElement>())
		{
			Object.DestroyImmediate(element);
		}

		TasksWindowBuilder.SetRect((RectTransform)arkButton.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(110, -420), new Vector2(180, 110));
		RectTransform label = arkButton.GetComponentInChildren<TMP_Text>(true).rectTransform;
		label.anchorMin = new Vector2(0.45f, 0);
		label.anchorMax = Vector2.one;
		label.offsetMin = Vector2.zero;
		label.offsetMax = Vector2.zero;

		GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
		icon.layer = hud.layer;
		RectTransform iconRect = (RectTransform)icon.transform;
		iconRect.SetParent(arkButton.transform, false);
		TasksWindowBuilder.SetRect(iconRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(72, 72));
		Image iconImage = icon.GetComponent<Image>();
		iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChestSpritePath);
		iconImage.preserveAspect = true;
		iconImage.raycastTarget = false;
		arkButton.gameObject.SetActive(false);

		TMP_Text timer = TasksWindowBuilder.CreateText("BoostTimerText", hudRect, textTemplate, 32, GoldColor, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(timer.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -490), new Vector2(420, 50));
		timer.outlineWidth = 0.2f;
		timer.outlineColor = new Color32(8, 16, 24, 255);
		timer.text = "Mining x1.2  59:12";
		timer.gameObject.SetActive(false);

		BoostHudView view = hud.AddComponent<BoostHudView>();
		SerializedObject so = new SerializedObject(view);
		so.FindProperty("arkButton").objectReferenceValue = arkButton;
		so.FindProperty("boostTimerText").objectReferenceValue = timer;
		so.ApplyModifiedPropertiesWithoutUndo();
	}
}
