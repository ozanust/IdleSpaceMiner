using UnityEngine;

/// <summary>
/// Plays a short sprite effect in the world, e.g. an explosion or a scan ripple: steps through the
/// frames, fades out and removes itself. A single sprite flares up and fades instead.
/// </summary>
public class FxPlayer : MonoBehaviour
{
	// Above planets, ships and floating texts
	private const int SortingOrder = 12;

	private SpriteRenderer spriteRenderer;
	private Sprite[] frames;
	private float frameTime;
	private float fadeTime;
	private float age;
	private float size;

	public static void Play(Sprite[] frames, Vector3 position, float size, float frameTime = 0.09f, float fadeTime = 0.25f)
	{
		if (frames == null || frames.Length == 0 || frames[0] == null)
		{
			return;
		}

		GameObject go = new GameObject("Fx", typeof(SpriteRenderer));
		go.transform.position = position;
		FxPlayer player = go.AddComponent<FxPlayer>();
		player.spriteRenderer = go.GetComponent<SpriteRenderer>();
		player.spriteRenderer.sortingOrder = SortingOrder;
		player.frames = frames;
		player.frameTime = frameTime;
		player.fadeTime = fadeTime;
		player.size = size;
		player.SetFrame(0);
	}

	public static void Play(Sprite sprite, Vector3 position, float size, float duration = 0.7f)
	{
		Play(new[] { sprite }, position, size, duration * 0.4f, duration * 0.6f);
	}

	private void SetFrame(int index)
	{
		Sprite sprite = frames[index];
		spriteRenderer.sprite = sprite;
		float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
		transform.localScale = Vector3.one * (spriteSize > 0f ? size / spriteSize : 1f);
	}

	private void Update()
	{
		age += Time.deltaTime;
		int frame = Mathf.Min(Mathf.FloorToInt(age / frameTime), frames.Length - 1);
		SetFrame(frame);

		// A lone sprite swells a little while it shows
		if (frames.Length == 1)
		{
			transform.localScale *= 0.8f + 0.4f * Mathf.Clamp01(age / frameTime);
		}

		float fadeStart = frameTime * frames.Length;
		if (age > fadeStart)
		{
			float alpha = 1f - (age - fadeStart) / fadeTime;
			if (alpha <= 0f)
			{
				Destroy(gameObject);
				return;
			}

			spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
		}
	}
}
