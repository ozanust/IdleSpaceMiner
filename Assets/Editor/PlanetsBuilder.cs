using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds the planets revealed by Telescope 1 (Verr, Newton, Widow) to GameScene as Planet.prefab
/// instances, styled like the existing planets, and registers them in SpaceView in planet id order.
/// Running it again only moves planets that already exist back to their default position.
/// </summary>
public static class PlanetsBuilder
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";
	private const string PlanetPrefabPath = "Assets/Prefabs/Planets/Planet.prefab";

	// World positions around the mothership at (0, 0), inside the default camera view
	private static readonly (string name, Vector3 position)[] NewPlanets =
	{
		("Planet (4)", new Vector3(-11f, -5f, 0f)),    // Verr
		("Planet (5)", new Vector3(11.5f, 6f, 0f)),    // Newton
		("Planet (6)", new Vector3(-9.5f, -19f, 0f)),  // Widow
	};

	// Existing planets that sat outside the default camera view
	private static readonly (string name, Vector3 position)[] MovedPlanets =
	{
		("Planet (2)", new Vector3(1.5f, -13.5f, 0f)), // Anadius, was behind the nav bar
	};

	[MenuItem("Tools/Planets/Add Telescope 1 Planets")]
	public static void BuildInOpenScene()
	{
		Build();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: opens GameScene, adds the planets and saves the scene.</summary>
	public static void BuildAndSaveScene()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Build();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Build()
	{
		SpaceView spaceView = Object.FindFirstObjectByType<SpaceView>(FindObjectsInactive.Include);
		SerializedObject so = new SerializedObject(spaceView);
		SerializedProperty planets = so.FindProperty("planets");

		// Copy the scene overrides (tag, trigger, sorting, locked colour) from the first planet
		PlanetSpaceView template = (PlanetSpaceView)planets.GetArrayElementAtIndex(0).objectReferenceValue;
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlanetPrefabPath);

		foreach (var (name, position) in NewPlanets)
		{
			GameObject existing = GameObject.Find(name);
			GameObject planet = existing != null ? existing : (GameObject)PrefabUtility.InstantiatePrefab(prefab, template.gameObject.scene);

			planet.name = name;
			planet.transform.SetParent(template.transform.parent, false);
			planet.transform.position = position;
			planet.transform.localScale = template.transform.localScale;

			// Cargo ships detect arrival by the Planet tag and a trigger collider,
			// and the sprite sorts below the price label
			planet.tag = template.tag;
			planet.GetComponent<Collider2D>().isTrigger = template.GetComponent<Collider2D>().isTrigger;
			SpriteRenderer sprite = planet.GetComponent<SpriteRenderer>();
			SpriteRenderer templateSprite = template.GetComponent<SpriteRenderer>();
			sprite.color = templateSprite.color;
			sprite.sortingOrder = templateSprite.sortingOrder;

			// Script changes to prefab instances are only saved once recorded as overrides
			PrefabUtility.RecordPrefabInstancePropertyModifications(planet);
			PrefabUtility.RecordPrefabInstancePropertyModifications(planet.transform);
			PrefabUtility.RecordPrefabInstancePropertyModifications(planet.GetComponent<Collider2D>());
			PrefabUtility.RecordPrefabInstancePropertyModifications(sprite);

			PlanetSpaceView view = planet.GetComponent<PlanetSpaceView>();
			if (!Contains(planets, view))
			{
				planets.arraySize++;
				planets.GetArrayElementAtIndex(planets.arraySize - 1).objectReferenceValue = view;
			}
		}

		so.ApplyModifiedPropertiesWithoutUndo();

		foreach (var (name, position) in MovedPlanets)
		{
			GameObject planet = GameObject.Find(name);
			if (planet != null)
			{
				planet.transform.position = position;
				PrefabUtility.RecordPrefabInstancePropertyModifications(planet.transform);
			}
		}
	}

	private static bool Contains(SerializedProperty array, Object value)
	{
		for (int i = 0; i < array.arraySize; i++)
		{
			if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
			{
				return true;
			}
		}

		return false;
	}
}
