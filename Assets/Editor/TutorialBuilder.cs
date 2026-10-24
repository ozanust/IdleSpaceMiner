using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the tutorial overlay in GameScene on top of all other UI: hint box with continue and
/// skip buttons, a ring and an arrow. Running it again replaces what it built before.
/// </summary>
public static class TutorialBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string ViewName = "TutorialView";
	private const string HintBackgroundPath = "TasksView/Panel";
	private const string RingSpritePath = "Assets/GFX/Sprites/UI/SciFi/ui_ring.png";
	private const string ArrowSpritePath = "Assets/GFX/Sprites/rightArrow.png";

	private static readonly Color AccentColor = new Color(0.35f, 0.95f, 1f, 1f);
	private static readonly Color MutedColor = new Color(0.55f, 0.85f, 0.92f, 1f);
	private static readonly Color BackingColor = new Color(0.02f, 0.06f, 0.1f, 0.94f);

	[MenuItem("Tools/UI/Build Tutorial Overlay")]
	public static void BuildInOpenScene()
	{
		Build();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: opens GameScene, builds the overlay and saves the scene.</summary>
	public static void BuildAndSaveScene()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Build();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Build()
	{
		Transform canvas = Object.FindAnyObjectByType<NavBarView>(FindObjectsInactive.Include).transform.parent;
		Transform old = canvas.Find(ViewName);
		if (old != null)
		{
			Object.DestroyImmediate(old.gameObject);
		}

		TMP_Text textTemplate = canvas.Find(TasksWindowBuilder.TextTemplatePath).GetComponent<TMP_Text>();

		RectTransform view = CreateRect(ViewName, canvas);
		TasksWindowBuilder.Stretch(view);
		view.SetAsLastSibling();

		RectTransform root = CreateRect("Root", view);
		TasksWindowBuilder.Stretch(root);

		Image ring = CreateRect("Ring", root).gameObject.AddComponent<Image>();
		ring.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RingSpritePath);
		ring.preserveAspect = true;
		ring.raycastTarget = false;
		TasksWindowBuilder.SetRect(ring.rectTransform, Center, Center, Center, Vector2.zero, new Vector2(240, 240));

		Image pointer = CreateRect("Pointer", root).gameObject.AddComponent<Image>();
		pointer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);
		pointer.color = AccentColor;
		pointer.preserveAspect = true;
		pointer.raycastTarget = false;
		TasksWindowBuilder.SetRect(pointer.rectTransform, Center, Center, Center, Vector2.zero, new Vector2(130, 130));

		// Hint box, the view moves it to the top or bottom edge
		RectTransform hint = CreateRect("HintPanel", root);
		TasksWindowBuilder.SetRect(hint, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(780, 260));
		// The frame sprite is see-through, so a dark backing keeps the text readable over planets
		Image backing = CreateRect("Backing", hint).gameObject.AddComponent<Image>();
		backing.color = BackingColor;
		backing.raycastTarget = false;
		TasksWindowBuilder.Stretch(backing.rectTransform);
		backing.rectTransform.offsetMin = new Vector2(24, 20);
		backing.rectTransform.offsetMax = new Vector2(-24, -20);

		Image hintBackground = CreateRect("Frame", hint).gameObject.AddComponent<Image>();
		TasksWindowBuilder.Stretch(hintBackground.rectTransform);
		Image backgroundTemplate = canvas.Find(HintBackgroundPath).GetComponent<Image>();
		hintBackground.sprite = backgroundTemplate.sprite;
		hintBackground.type = backgroundTemplate.type;
		hintBackground.color = backgroundTemplate.color;
		hintBackground.raycastTarget = false;

		TMP_Text hintText = TasksWindowBuilder.CreateText("HintText", hint, textTemplate, 38, Color.white, TextAlignmentOptions.TopLeft);
		RectTransform hintTextRect = hintText.rectTransform;
		hintTextRect.anchorMin = Vector2.zero;
		hintTextRect.anchorMax = Vector2.one;
		hintTextRect.offsetMin = new Vector2(44, 92);
		hintTextRect.offsetMax = new Vector2(-44, -34);
		hintText.textWrappingMode = TextWrappingModes.Normal;
		hintText.overflowMode = TextOverflowModes.Overflow;
		hintText.enableAutoSizing = true;
		hintText.fontSizeMin = 26;
		hintText.fontSizeMax = 38;
		hintText.text = "Welcome, Commander! Tap Balor to buy your first planet.";

		Button continueButton = TasksWindowBuilder.CloneButton(canvas.Find(TasksWindowBuilder.ClaimButtonTemplatePath), hint, "ContinueButton", "Continue");
		TasksWindowBuilder.SetRect((RectTransform)continueButton.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-34, 22), new Vector2(240, 72));

		TMP_Text skipText = TasksWindowBuilder.CreateText("SkipButton", hint, textTemplate, 26, MutedColor, TextAlignmentOptions.Left);
		TasksWindowBuilder.SetRect(skipText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(44, 26), new Vector2(240, 60));
		skipText.text = "Skip tutorial";
		skipText.raycastTarget = true;
		Button skipButton = skipText.gameObject.AddComponent<Button>();
		skipButton.targetGraphic = skipText;

		TutorialView component = view.gameObject.AddComponent<TutorialView>();
		SerializedObject so = new SerializedObject(component);
		so.FindProperty("root").objectReferenceValue = root.gameObject;
		so.FindProperty("hintPanel").objectReferenceValue = hint;
		so.FindProperty("hintText").objectReferenceValue = hintText;
		so.FindProperty("continueButton").objectReferenceValue = continueButton;
		so.FindProperty("skipButton").objectReferenceValue = skipButton;
		so.FindProperty("ring").objectReferenceValue = ring.rectTransform;
		so.FindProperty("pointer").objectReferenceValue = pointer.rectTransform;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

	private static RectTransform CreateRect(string name, Transform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.layer = parent.gameObject.layer;
		go.transform.SetParent(parent, false);
		return (RectTransform)go.transform;
	}
}
