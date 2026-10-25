using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Places the planets in GameScene around the mothership: the first four in the starting view,
/// then each telescope tier of three on a wider ring, so later planets are found by panning.
/// Positions are prefab overrides on the planets SpaceView lists, in planet id order.
/// </summary>
public static class PlanetLayoutTool
{
	private const string ScenePath = "Assets/Scenes/GameScene.unity";

	private static readonly Vector2[] Positions =
	{
		// Starting view
		new Vector2(-9.9f, 13.1f),
		new Vector2(10.09f, 21.96f),
		new Vector2(1.5f, -13.5f),
		new Vector2(10.1f, -18.4f),
		// Telescope 1, just past the edges
		new Vector2(-22f, 4f),
		new Vector2(24f, 2f),
		new Vector2(-6f, -32f),
		// Telescope 2
		new Vector2(16f, 34f),
		new Vector2(-30f, 24f),
		new Vector2(28f, -26f),
		// Telescope 3
		new Vector2(-36f, -28f),
		new Vector2(40f, 20f),
		new Vector2(-4f, 48f),
		// Telescope 4
		new Vector2(46f, -34f),
		new Vector2(-48f, 30f),
		new Vector2(8f, -58f)
	};

	[MenuItem("Tools/Space/Spread Planets")]
	public static void ApplyInOpenScene()
	{
		Apply();
		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
	}

	/// <summary>Batch mode entry point: opens GameScene, moves the planets and saves.</summary>
	public static void ApplyAndSave()
	{
		EditorSceneManager.OpenScene(ScenePath);
		Apply();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
	}

	private static void Apply()
	{
		SpaceView spaceView = Object.FindAnyObjectByType<SpaceView>(FindObjectsInactive.Include);
		SerializedProperty planets = new SerializedObject(spaceView).FindProperty("planets");
		for (int i = 0; i < planets.arraySize && i < Positions.Length; i++)
		{
			Transform planet = ((Component)planets.GetArrayElementAtIndex(i).objectReferenceValue).transform;
			Undo.RecordObject(planet, "Spread Planets");
			planet.position = new Vector3(Positions[i].x, Positions[i].y, planet.position.z);
			PrefabUtility.RecordPrefabInstancePropertyModifications(planet);
		}
	}
}
