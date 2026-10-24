using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the managers window in GameScene: a copy of the research window with a header for
/// slots, hiring and status messages above a list of manager rows. Shares the tasks window
/// helpers and templates. Running it again replaces what it built before.
/// </summary>
public static class ManagersWindowBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string WindowName = "ManagersView";
	private const string DotSpritePath = "Assets/Sci-Fi Game UI 8/MAIN/PNG/OPTION BUTTON/option buton active.png";

	private const float HeaderHeight = 330f;
	private const float RowHeight = 230f;
	private const float ActionWidth = 260f;
	private const int MaxStars = 7;

	private static readonly Color MutedColor = new Color(0.55f, 0.85f, 0.92f, 1f);
	private static readonly Color SecondaryColor = new Color(0.75f, 0.65f, 1f, 1f);

	[MenuItem("Tools/UI/Build Managers Window")]
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
		Transform old = canvas.Find(WindowName);
		if (old != null)
		{
			Object.DestroyImmediate(old.gameObject);
		}

		TMP_Text textTemplate = canvas.Find(TasksWindowBuilder.TextTemplatePath).GetComponent<TMP_Text>();
		Transform buttonTemplate = canvas.Find(TasksWindowBuilder.ClaimButtonTemplatePath);

		Transform panel;
		Button closeButton;
		ScrollRect scroll;
		GameObject window = TasksWindowBuilder.CreateListWindow(canvas, WindowName, "Managers", out panel, out closeButton, out scroll);

		// The header takes the top of the scroll area
		RectTransform scrollRect = (RectTransform)scroll.transform;
		GameObject headerObject = new GameObject("Header", typeof(RectTransform));
		headerObject.layer = canvas.gameObject.layer;
		RectTransform header = (RectTransform)headerObject.transform;
		header.SetParent(scrollRect.parent, false);
		header.anchorMin = new Vector2(scrollRect.anchorMin.x, scrollRect.anchorMax.y);
		header.anchorMax = scrollRect.anchorMax;
		header.pivot = new Vector2(0.5f, 1);
		header.offsetMin = new Vector2(scrollRect.offsetMin.x, scrollRect.offsetMax.y - HeaderHeight);
		header.offsetMax = scrollRect.offsetMax;
		scrollRect.offsetMax = new Vector2(scrollRect.offsetMax.x, scrollRect.offsetMax.y - HeaderHeight);

		float pad = TasksWindowBuilder.RowPadding;

		TMP_Text slotsText = TasksWindowBuilder.CreateText("SlotsText", header, textTemplate, 40, Color.white, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(slotsText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(pad, -24), new Vector2(400, 60));
		slotsText.text = "Slots 1 / 2";

		Button buySlotButton = TasksWindowBuilder.CloneButton(buttonTemplate, header, "BuySlotButton", "+1 Slot  50 DM");
		TasksWindowBuilder.SetRect((RectTransform)buySlotButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-pad, -12), new Vector2(340, 84));
		AutoSize(buySlotButton);

		// Hire buttons, one per school
		GameObject hireRow = new GameObject("HireButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
		hireRow.layer = canvas.gameObject.layer;
		RectTransform hireRect = (RectTransform)hireRow.transform;
		hireRect.SetParent(header, false);
		TasksWindowBuilder.SetRect(hireRect, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(-pad * 2, 140));
		HorizontalLayoutGroup hireLayout = hireRow.GetComponent<HorizontalLayoutGroup>();
		hireLayout.spacing = 20;
		hireLayout.childControlWidth = true;
		hireLayout.childControlHeight = true;
		hireLayout.childForceExpandWidth = true;
		hireLayout.childForceExpandHeight = true;

		string[] schools = { "Public School\n50 DM", "Private School\n300 DM", "Brilliant Academy\n500 DM" };
		Button[] hireButtons = new Button[schools.Length];
		for (int i = 0; i < schools.Length; i++)
		{
			hireButtons[i] = TasksWindowBuilder.CloneButton(buttonTemplate, hireRect, "Hire" + i, schools[i]);
			foreach (LayoutElement element in hireButtons[i].GetComponents<LayoutElement>())
			{
				Object.DestroyImmediate(element);
			}

			AutoSize(hireButtons[i]);
		}

		TMP_Text statusText = TasksWindowBuilder.CreateText("StatusText", header, textTemplate, 32, TasksWindowBuilder.RewardColor, TextAlignmentOptions.Center);
		TasksWindowBuilder.SetRect(statusText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(-pad * 2, 50));
		statusText.text = "";

		ManagerItem prototype = BuildRow(canvas, scroll.content, textTemplate, buttonTemplate);

		TMP_Text emptyText = TasksWindowBuilder.CreateText("EmptyText", scroll.transform, textTemplate, 36, Color.white, TextAlignmentOptions.Center);
		TasksWindowBuilder.Stretch(emptyText.rectTransform);
		emptyText.textWrappingMode = TextWrappingModes.Normal;
		emptyText.text = "No managers yet.\nHire one with Dark Matter.";
		emptyText.gameObject.SetActive(false);

		ManagersView view = window.AddComponent<ManagersView>();
		SerializedObject so = new SerializedObject(view);
		so.FindProperty("panel").objectReferenceValue = panel.gameObject;
		so.FindProperty("closeButton").objectReferenceValue = closeButton;
		so.FindProperty("itemContainer").objectReferenceValue = scroll.content;
		so.FindProperty("itemPrototype").objectReferenceValue = prototype;
		so.FindProperty("emptyText").objectReferenceValue = emptyText;
		so.FindProperty("slotsText").objectReferenceValue = slotsText;
		so.FindProperty("statusText").objectReferenceValue = statusText;
		so.FindProperty("buySlotButton").objectReferenceValue = buySlotButton;
		SerializedProperty hire = so.FindProperty("hireButtons");
		hire.arraySize = hireButtons.Length;
		for (int i = 0; i < hireButtons.Length; i++)
		{
			hire.GetArrayElementAtIndex(i).objectReferenceValue = hireButtons[i];
		}

		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static ManagerItem BuildRow(Transform canvas, Transform content, TMP_Text textTemplate, Transform buttonTemplate)
	{
		Image backgroundTemplate = canvas.Find(TasksWindowBuilder.RowBackgroundTemplatePath).GetComponent<Image>();
		float pad = TasksWindowBuilder.RowPadding;

		GameObject row = new GameObject("ManagerItem", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
		row.layer = canvas.gameObject.layer;
		RectTransform rowRect = (RectTransform)row.transform;
		rowRect.SetParent(content, false);

		Image background = row.GetComponent<Image>();
		background.sprite = backgroundTemplate.sprite;
		background.type = backgroundTemplate.type;
		background.color = backgroundTemplate.color;
		row.GetComponent<LayoutElement>().preferredHeight = RowHeight;

		// Rank as a row of lit dots
		Sprite dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DotSpritePath);
		if (dotSprite == null)
		{
			dotSprite = canvas.Find(TasksWindowBuilder.BadgeTemplatePath).GetComponent<Image>().sprite;
		}

		Image[] dots = new Image[MaxStars];
		for (int i = 0; i < MaxStars; i++)
		{
			GameObject dot = new GameObject("RankDot" + i, typeof(RectTransform), typeof(Image));
			dot.layer = row.layer;
			RectTransform dotRect = (RectTransform)dot.transform;
			dotRect.SetParent(rowRect, false);
			TasksWindowBuilder.SetRect(dotRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(pad + i * 46, -22), new Vector2(38, 38));
			dots[i] = dot.GetComponent<Image>();
			dots[i].sprite = dotSprite;
			dots[i].preserveAspect = true;
			dots[i].raycastTarget = false;
		}

		float textWidth = -(pad * 3 + ActionWidth);

		TMP_Text primaryText = TasksWindowBuilder.CreateText("PrimaryText", rowRect, textTemplate, 38, Color.white, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(primaryText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(pad, -70), new Vector2(textWidth, 50));
		primaryText.text = "Mining x1.5";

		TMP_Text secondaryText = TasksWindowBuilder.CreateText("SecondaryText", rowRect, textTemplate, 30, SecondaryColor, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(secondaryText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(pad, -124), new Vector2(textWidth, 42));
		secondaryText.text = "Secondary skill at 3 stars";

		TMP_Text assignmentText = TasksWindowBuilder.CreateText("AssignmentText", rowRect, textTemplate, 30, MutedColor, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(assignmentText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(pad, -172), new Vector2(textWidth, 42));
		assignmentText.text = "Idle";

		Button actionButton = TasksWindowBuilder.CloneButton(buttonTemplate, rowRect, "ActionButton", "Unassign");
		TasksWindowBuilder.SetRect((RectTransform)actionButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-pad, -22), new Vector2(ActionWidth, 84));
		AutoSize(actionButton);

		Button promoteButton = TasksWindowBuilder.CloneButton(buttonTemplate, rowRect, "PromoteButton", "Promote");
		TasksWindowBuilder.SetRect((RectTransform)promoteButton.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-pad, 22), new Vector2(ActionWidth, 84));
		AutoSize(promoteButton);

		ManagerItem item = row.AddComponent<ManagerItem>();
		SerializedObject so = new SerializedObject(item);
		SerializedProperty dotsProperty = so.FindProperty("rankDots");
		dotsProperty.arraySize = dots.Length;
		for (int i = 0; i < dots.Length; i++)
		{
			dotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
		}

		so.FindProperty("primaryText").objectReferenceValue = primaryText;
		so.FindProperty("secondaryText").objectReferenceValue = secondaryText;
		so.FindProperty("assignmentText").objectReferenceValue = assignmentText;
		so.FindProperty("actionButton").objectReferenceValue = actionButton;
		so.FindProperty("promoteButton").objectReferenceValue = promoteButton;
		so.ApplyModifiedPropertiesWithoutUndo();

		return item;
	}

	private static void AutoSize(Button button)
	{
		TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
		label.enableAutoSizing = true;
		label.fontSizeMin = 20;
		label.fontSizeMax = 34;
		label.textWrappingMode = TextWrappingModes.Normal;
		label.alignment = TextAlignmentOptions.Center;
	}
}
