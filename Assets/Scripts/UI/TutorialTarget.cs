using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a UI element or world object the tutorial can point at. Views tag their own
/// objects with Mark, which also works for objects created at runtime or still inactive.
/// </summary>
public class TutorialTarget : MonoBehaviour
{
	private static readonly List<TutorialTarget> targets = new List<TutorialTarget>();

	[SerializeField] private TutorialTargetId id;

	public TutorialTargetId Id => id;

	public static void Mark(GameObject go, TutorialTargetId id)
	{
		TutorialTarget target = go.GetComponent<TutorialTarget>();
		if (target == null)
		{
			target = go.AddComponent<TutorialTarget>();
		}

		target.id = id;
		if (!targets.Contains(target))
		{
			targets.Add(target);
		}
	}

	/// <summary>First target with the id that is on screen right now, or null.</summary>
	public static TutorialTarget FindVisible(TutorialTargetId id)
	{
		for (int i = targets.Count - 1; i >= 0; i--)
		{
			// Destroyed objects that were never active skip OnDestroy, so prune them here
			if (targets[i] == null)
			{
				targets.RemoveAt(i);
			}
		}

		foreach (TutorialTarget target in targets)
		{
			if (target.id == id && target.IsVisible())
			{
				return target;
			}
		}

		return null;
	}

	private bool IsVisible()
	{
		if (!gameObject.activeInHierarchy)
		{
			return false;
		}

		// Hidden planets keep their object active and only switch the sprite off
		SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
		return transform is RectTransform || sprite == null || sprite.enabled;
	}

	private void OnDestroy()
	{
		targets.Remove(this);
	}
}
