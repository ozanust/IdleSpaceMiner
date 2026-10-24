using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays out the production UI: smelter/crafter cards (recipe line, progress, buttons, locked state),
/// recipe picker cards (ingredients with amounts, time, unlock price) and the recipe picker's
/// grid and title bar. Rearranges existing objects so script references stay intact.
/// Run after Tools > UI > Apply Sci-Fi Theme.
/// </summary>
public static class ProductionUiBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string PrefabFolder = "Assets/Prefabs/UIPrefabs/";
	private const string ArrowSpritePath = "Assets/GFX/Sprites/rightArrow.png";
	private const string BuySpritePath = "Assets/Sci-Fi Game UI 8/MAIN/PNG/Buy button.png";

	private static readonly Color MutedColor = new Color(0.55f, 0.85f, 0.92f, 1f);
	private static readonly Color TextColor = new Color(0.88f, 0.97f, 1f, 1f);

	[MenuItem("Tools/UI/Build Production UI")]
	public static void BuildInOpenScene()
	{
		BuildPrefabs();
		BuildScene();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point.</summary>
	public static void BuildAndSave()
	{
		BuildPrefabs();
		EditorSceneManager.OpenScene(ScenePath);
		BuildScene();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void BuildPrefabs()
	{
		EditPrefab("SmeltItem", root => BuildMachineCard(root, "SMELTER", "Build Smelter"));
		EditPrefab("CraftItem", root => BuildMachineCard(root, "CRAFTER", "Build Crafter"));
		EditPrefab("LockedRecipeItem", BuildRecipeCard);
	}

	private static void EditPrefab(string name, System.Action<GameObject> build)
	{
		string path = PrefabFolder + name + ".prefab";
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		build(root);
		PrefabUtility.SaveAsPrefabAsset(root, path);
		PrefabUtility.UnloadPrefabContents(root);
	}

	// --- Smelter / crafter card (415 x 450) ---

	private static void BuildMachineCard(GameObject root, string title, string buildText)
	{
		Transform unlocked = root.transform.Find("SmelterUnlockedPanel");
		Transform locked = root.transform.Find("SmelterLockedPanel");

		TMP_Text cardTitle = GetOrCreateText(unlocked, "CardTitle");
		Place(cardTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(355, 40));
		Style(cardTitle, title, 26, MutedColor, TextAlignmentOptions.Center);
		cardTitle.characterSpacing = 6;

		// Recipe line: ingredient(s) -> result
		RectTransform images = (RectTransform)unlocked.Find("SelectedRecipeImages");
		Place(images, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(380, 110));
		RectTransform source = (RectTransform)images.Find("SourceResourceImage");
		RectTransform arrow = (RectTransform)images.Find("ArrowImage");
		RectTransform target = (RectTransform)images.Find("TargetAlloyImage");
		Icon(source, -105, 96);
		Icon(target, 105, 96);
		Icon(arrow, 0, 48);
		arrow.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);

		CrafterItem crafter = root.GetComponent<CrafterItem>();
		if (crafter != null)
		{
			RectTransform second = (RectTransform)images.Find("SecondSourceResourceImage");
			if (second == null)
			{
				second = (RectTransform)Object.Instantiate(source.gameObject, images, false).transform;
				second.name = "SecondSourceResourceImage";
				second.SetSiblingIndex(source.GetSiblingIndex() + 1);
			}

			second.gameObject.SetActive(false);
			SerializedObject so = new SerializedObject(crafter);
			so.FindProperty("secondSourceResourceImage").objectReferenceValue = second.GetComponent<Image>();
			so.FindProperty("arrowImage").objectReferenceValue = arrow;
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		TMP_Text noRecipe = unlocked.Find("NoRecipeSelectedText").GetComponent<TMP_Text>();
		Place(noRecipe.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(355, 110));
		Style(noRecipe, "No recipe selected", 32, MutedColor, TextAlignmentOptions.Center);

		// Progress bar with the remove button at its end and the time under it
		RectTransform slider = (RectTransform)unlocked.Find("Slider");
		slider.anchorMin = new Vector2(0, 0);
		slider.anchorMax = new Vector2(1, 0);
		slider.pivot = new Vector2(0.5f, 0);
		slider.offsetMin = new Vector2(30, 160);
		slider.offsetMax = new Vector2(-96, 196);
		Slider sliderComponent = slider.GetComponent<Slider>();
		sliderComponent.interactable = false;
		sliderComponent.transition = Selectable.Transition.None;
		Transform handleArea = slider.Find("Handle Slide Area");
		if (handleArea != null)
		{
			handleArea.gameObject.SetActive(false);
		}

		RectTransform remove = (RectTransform)slider.Find("RemoveRecipeButton");
		remove.anchorMin = remove.anchorMax = new Vector2(1, 0.5f);
		remove.pivot = new Vector2(0, 0.5f);
		remove.anchoredPosition = new Vector2(14, 0);
		remove.sizeDelta = new Vector2(56, 56);

		TMP_Text remaining = slider.Find("RemainingTime").GetComponent<TMP_Text>();
		RectTransform remainingRect = remaining.rectTransform;
		remainingRect.anchorMin = remainingRect.anchorMax = new Vector2(0.5f, 0);
		remainingRect.pivot = new Vector2(0.5f, 1);
		remainingRect.anchoredPosition = new Vector2(0, -6);
		remainingRect.sizeDelta = new Vector2(320, 40);
		Style(remaining, "Idle", 28, TextColor, TextAlignmentOptions.Center);

		RectTransform recipeButton = (RectTransform)unlocked.Find("SetRecipeButton");
		Place(recipeButton, new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(280, 76));
		TMP_Text recipeLabel = recipeButton.GetComponentInChildren<TMP_Text>(true);
		Style(recipeLabel, "Set recipe", 30, TextColor, TextAlignmentOptions.Center);
		recipeLabel.fontStyle = FontStyles.Bold;

		// Locked: lock, "Build ..." and the price on a buy plate
		RectTransform lockImage = (RectTransform)locked.Find("LockImage");
		Place(lockImage, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(160, 160));
		lockImage.GetComponent<Image>().preserveAspect = true;

		TMP_Text build = locked.Find("BuildSmelterText").GetComponent<TMP_Text>();
		Place(build.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(355, 50));
		Style(build, buildText, 36, TextColor, TextAlignmentOptions.Center);
		build.fontStyle = FontStyles.Bold;

		Image plate = GetOrCreateImage(locked, "PriceBackground");
		Place(plate.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(320, 96));
		plate.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BuySpritePath);
		plate.transform.SetSiblingIndex(locked.Find("PriceText").GetSiblingIndex());

		TMP_Text price = locked.Find("PriceText").GetComponent<TMP_Text>();
		Place(price.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(320, 70));
		Style(price, price.text, 40, TextColor, TextAlignmentOptions.Center);
		price.fontStyle = FontStyles.Bold;
	}

	private static void Icon(RectTransform icon, float x, float size)
	{
		icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
		icon.anchoredPosition = new Vector2(x, 0);
		icon.sizeDelta = new Vector2(size, size);
		icon.GetComponent<Image>().preserveAspect = true;
	}

	// --- Recipe picker card (440 x 380) ---

	private static void BuildRecipeCard(GameObject root)
	{
		Transform t = root.transform;

		TMP_Text title = t.Find("TitleText").GetComponent<TMP_Text>();
		Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(380, 50));
		Style(title, title.text, 38, TextColor, TextAlignmentOptions.Center);
		title.fontStyle = FontStyles.Bold;

		// Lock badge in the corner while locked
		RectTransform lockIcon = (RectTransform)t.Find("Icon");
		Place(lockIcon, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(52, 52));
		lockIcon.pivot = new Vector2(1, 1);
		lockIcon.anchoredPosition = new Vector2(-24, -24);
		lockIcon.GetComponent<Image>().preserveAspect = true;

		RectTransform images = (RectTransform)t.Find("SelectedRecipeImages");
		Place(images, new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(420, 150));
		if (images.GetComponent<CanvasGroup>() == null)
		{
			images.gameObject.AddComponent<CanvasGroup>();
		}

		foreach (string name in new[] { "SourceResourceImage", "SecondSourceResourceImage" })
		{
			RectTransform icon = (RectTransform)images.Find(name);
			icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
			icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, 18);
			icon.sizeDelta = new Vector2(90, 90);
			icon.GetComponent<Image>().preserveAspect = true;

			TMP_Text amount = icon.GetComponentInChildren<TMP_Text>(true);
			RectTransform amountRect = amount.rectTransform;
			amountRect.anchorMin = amountRect.anchorMax = new Vector2(0.5f, 0);
			amountRect.pivot = new Vector2(0.5f, 1);
			amountRect.anchoredPosition = new Vector2(0, -2);
			amountRect.sizeDelta = new Vector2(120, 36);
			Style(amount, amount.text, 28, TextColor, TextAlignmentOptions.Center);
		}

		RectTransform arrow = (RectTransform)images.Find("ArrowImage");
		arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(0.5f, 0.5f);
		arrow.anchoredPosition = new Vector2(arrow.anchoredPosition.x, 18);
		arrow.sizeDelta = new Vector2(44, 44);
		arrow.GetComponent<Image>().preserveAspect = true;

		RectTransform target = (RectTransform)images.Find("TargetAlloyImage");
		target.anchorMin = target.anchorMax = target.pivot = new Vector2(0.5f, 0.5f);
		target.anchoredPosition = new Vector2(target.anchoredPosition.x, 18);
		target.sizeDelta = new Vector2(100, 100);
		target.GetComponent<Image>().preserveAspect = true;

		TMP_Text duration = t.Find("DurationText").GetComponent<TMP_Text>();
		Place(duration.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 134), new Vector2(380, 40));
		Style(duration, duration.text, 28, MutedColor, TextAlignmentOptions.Center);
		duration.gameObject.SetActive(true);

		// Unlock price on a buy plate at the bottom
		Image footer = GetOrCreateImage(t, "LockedFooter");
		Place(footer.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(330, 84));
		footer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BuySpritePath);
		footer.raycastTarget = false;

		TMP_Text price = t.Find("PriceText") != null ? t.Find("PriceText").GetComponent<TMP_Text>() : footer.GetComponentInChildren<TMP_Text>(true);
		price.transform.SetParent(footer.transform, false);
		price.rectTransform.anchorMin = Vector2.zero;
		price.rectTransform.anchorMax = Vector2.one;
		price.rectTransform.offsetMin = new Vector2(0, 22);
		price.rectTransform.offsetMax = Vector2.zero;
		Style(price, price.text, 34, TextColor, TextAlignmentOptions.Center);
		price.fontStyle = FontStyles.Bold;

		t.Find("UnlockText").gameObject.SetActive(false);

		SmeltRecipeItem item = root.GetComponent<SmeltRecipeItem>();
		SerializedObject so = new SerializedObject(item);
		so.FindProperty("arrowImage").objectReferenceValue = arrow;
		so.FindProperty("lockedFooter").objectReferenceValue = footer.gameObject;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	// --- Scene ---

	private static void BuildScene()
	{
		Transform canvas = Object.FindFirstObjectByType<NavBarView>(FindObjectsInactive.Include).transform.parent;
		SmeltRecipesView recipesView = canvas.GetComponentInChildren<SmeltRecipesView>(true);
		Transform panel = recipesView.transform.Find("Panel");

		foreach (string content in new[] { "RecipesLayout/Viewport/SmeltContent", "RecipesLayout/Viewport/CraftContent" })
		{
			Transform container = panel.Find(content);
			GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
			grid.cellSize = new Vector2(440, 380);
			grid.spacing = new Vector2(24, 24);
			grid.padding = new RectOffset(8, 8, 8, 8);
			grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			grid.constraintCount = 2;
			grid.childAlignment = TextAnchor.UpperCenter;

			// Cards placed by hand in the scene were never wired up, the view creates the real ones
			foreach (Transform child in container)
			{
				child.gameObject.SetActive(false);
			}
		}

		// Title bar and close button like the other windows
		if (panel.Find("TitleBG") == null)
		{
			Transform template = canvas.Find("ResearchView/Panel/TitleBG");
			GameObject titleBar = Object.Instantiate(template.gameObject, panel, false);
			titleBar.name = "TitleBG";
		}

		Transform titleBg = panel.Find("TitleBG");
		Button close = titleBg.Find("CloseButton").GetComponent<Button>();
		close.onClick = new Button.ButtonClickedEvent();
		TMP_Text titleText = titleBg.Find("ViewTitle").GetComponent<TMP_Text>();
		titleText.text = "Smelt Recipes";

		SerializedObject so = new SerializedObject(recipesView);
		so.FindProperty("titleText").objectReferenceValue = titleText;
		so.FindProperty("closeButton").objectReferenceValue = close;
		so.ApplyModifiedPropertiesWithoutUndo();

		// The scene's machine cards are prefab instances, their child layout should follow the prefab
		ProductionView production = canvas.GetComponentInChildren<ProductionView>(true);
		foreach (MonoBehaviour card in production.GetComponentsInChildren<MonoBehaviour>(true))
		{
			if ((card is SmelterItem || card is CrafterItem) && PrefabUtility.IsOutermostPrefabInstanceRoot(card.gameObject))
			{
				foreach (Component component in card.GetComponentsInChildren<Component>(true))
				{
					if (component.gameObject != card.gameObject && (component is RectTransform || component is Graphic))
					{
						PrefabUtility.RevertObjectOverride(component, InteractionMode.AutomatedAction);
					}
				}
			}
		}
	}

	// --- Helpers ---

	private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
	{
		rect.anchorMin = rect.anchorMax = anchor;
		rect.pivot = new Vector2(0.5f, anchor.y);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	private static void Style(TMP_Text text, string value, float size, Color color, TextAlignmentOptions alignment)
	{
		Object.DestroyImmediate(text.GetComponent<ContentSizeFitter>());
		text.text = value;
		text.fontSize = size;
		text.enableAutoSizing = false;
		text.color = color;
		text.alignment = alignment;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Ellipsis;
	}

	private static TMP_Text GetOrCreateText(Transform parent, string name)
	{
		Transform existing = parent.Find(name);
		if (existing != null)
		{
			return existing.GetComponent<TMP_Text>();
		}

		GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
		go.layer = parent.gameObject.layer;
		go.transform.SetParent(parent, false);
		TMP_Text text = go.GetComponent<TMP_Text>();
		text.raycastTarget = false;
		return text;
	}

	private static Image GetOrCreateImage(Transform parent, string name)
	{
		Transform existing = parent.Find(name);
		if (existing != null)
		{
			return existing.GetComponent<Image>();
		}

		GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
		go.layer = parent.gameObject.layer;
		go.transform.SetParent(parent, false);
		Image image = go.GetComponent<Image>();
		image.raycastTarget = false;
		image.preserveAspect = false;
		return image;
	}
}
