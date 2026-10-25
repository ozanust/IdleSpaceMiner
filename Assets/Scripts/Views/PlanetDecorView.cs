using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brings a planet to life: the surface spins slowly, some planet types glow, and bought planets
/// get moons on tilted orbits and sometimes a ring, both passing behind and in front of the planet.
/// The planet's own renderer keeps its state (sprite, tint, hidden) but stops drawing; a spinning
/// copy draws instead, so the price text and collider don't turn with it.
/// Each planet's look comes from its position, so it is the same every time.
/// </summary>
public class PlanetDecorView : MonoBehaviour
{
	// Planet types that glow, matched against the sprite name, with their glow colour
	private static readonly KeyValuePair<string, Color>[] GlowColors =
	{
		new KeyValuePair<string, Color>("lava", new Color(1f, 0.45f, 0.1f)),
		new KeyValuePair<string, Color>("toxic", new Color(0.5f, 1f, 0.25f)),
		new KeyValuePair<string, Color>("nebula", new Color(0.75f, 0.4f, 1f)),
		new KeyValuePair<string, Color>("pearl", new Color(1f, 0.85f, 0.95f)),
		new KeyValuePair<string, Color>("abyss", new Color(0.25f, 0.5f, 1f))
	};

	private static readonly Color[] MoonTints =
	{
		new Color(0.85f, 0.85f, 0.88f),
		new Color(0.9f, 0.78f, 0.65f),
		new Color(0.7f, 0.8f, 0.95f)
	};

	private static Sprite glowSprite;
	private static Sprite moonSprite;
	private static Sprite ringBackSprite;
	private static Sprite ringFrontSprite;

	private SpriteRenderer source;
	private SpriteRenderer surface;
	private SpriteRenderer glow;
	private SpriteRenderer ringBack;
	private SpriteRenderer ringFront;
	private readonly List<Moon> moons = new List<Moon>();

	private float radius;
	private float spinSpeed;
	private float glowPhase;
	private string glowSpriteName;

	private class Moon
	{
		public SpriteRenderer Renderer;
		public float OrbitRadius;
		// Height of the orbit ellipse against its width, the smaller the flatter it looks
		public float Flatten;
		public float Tilt;
		public float Speed;
		public float Angle;
		public float Size;
	}

	/// <summary>Adds the decoration to a planet whose root holds its SpriteRenderer.</summary>
	public static PlanetDecorView Attach(GameObject planet)
	{
		PlanetDecorView view = planet.GetComponent<PlanetDecorView>();
		return view != null ? view : planet.AddComponent<PlanetDecorView>();
	}

	private void Awake()
	{
		source = GetComponent<SpriteRenderer>();
		EnsureSprites();

		// Same look every run for the same planet
		Vector3 p = transform.position;
		System.Random random = new System.Random(Mathf.RoundToInt(p.x * 73f) * 397 ^ Mathf.RoundToInt(p.y * 131f));

		spinSpeed = Lerp(random, 2f, 5f) * (random.NextDouble() < 0.5 ? -1f : 1f);
		glowPhase = Lerp(random, 0f, Mathf.PI * 2f);

		surface = CreateChild("Surface", null);
		glow = CreateChild("Glow", glowSprite);

		// Moons: none, one or two
		int moonCount = random.NextDouble() < 0.35 ? 0 : (random.NextDouble() < 0.7 ? 1 : 2);
		float tilt = Lerp(random, -25f, 25f);
		for (int i = 0; i < moonCount; i++)
		{
			moons.Add(new Moon
			{
				Renderer = CreateChild("Moon", moonSprite),
				OrbitRadius = Lerp(random, 1.35f, 1.75f) + i * 0.3f,
				Flatten = Lerp(random, 0.25f, 0.4f),
				Tilt = tilt + Lerp(random, -10f, 10f),
				Speed = Lerp(random, 18f, 35f) / (1f + i * 0.6f) * (random.NextDouble() < 0.5 ? -1f : 1f),
				Angle = Lerp(random, 0f, 360f),
				Size = Lerp(random, 0.16f, 0.26f) / (1f + i * 0.4f)
			});
			moons[i].Renderer.color = MoonTints[random.Next(MoonTints.Length)];
		}

		// About one planet in four wears a ring
		if (random.NextDouble() < 0.25)
		{
			ringBack = CreateChild("RingBack", ringBackSprite);
			ringFront = CreateChild("RingFront", ringFrontSprite);
			Quaternion ringTilt = Quaternion.Euler(0f, 0f, tilt);
			ringBack.transform.localRotation = ringTilt;
			ringFront.transform.localRotation = ringTilt;
		}
	}

	private void LateUpdate()
	{
		// Copy whatever the planet view set, then draw it spinning instead
		source.forceRenderingOff = true;
		bool visible = source.enabled && source.sprite != null;
		bool owned = visible && source.color.r > 0.99f;

		surface.enabled = visible;
		if (!visible)
		{
			SetDecorVisible(false, false);
			return;
		}

		surface.sprite = source.sprite;
		surface.color = source.color;
		surface.sortingLayerID = source.sortingLayerID;
		surface.sortingOrder = source.sortingOrder;
		surface.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * spinSpeed);
		radius = source.sprite.bounds.extents.x;

		UpdateGlow();
		SetDecorVisible(true, owned);
		if (owned)
		{
			UpdateMoons();
			UpdateRing();
		}
	}

	private void UpdateGlow()
	{
		if (glowSpriteName != source.sprite.name)
		{
			// The planet icon is set after start, so pick the glow colour once it is known
			glowSpriteName = source.sprite.name;
			glow.color = Color.clear;
			foreach (KeyValuePair<string, Color> entry in GlowColors)
			{
				if (glowSpriteName.Contains(entry.Key))
				{
					glow.color = entry.Value;
				}
			}
		}

		if (glow.color.a <= 0f)
		{
			glow.enabled = false;
			return;
		}

		glow.enabled = true;
		float breath = Mathf.Sin(Time.time * 0.8f + glowPhase);
		glow.sortingOrder = source.sortingOrder - 1;
		glow.transform.localScale = Vector3.one * radius * (3.1f + 0.15f * breath) / glowSprite.bounds.size.x;

		// Dimmer while the planet is still for sale and tinted grey
		Color color = glow.color;
		color.a = (0.5f + 0.15f * breath) * source.color.r;
		glow.color = color;
	}

	private void UpdateMoons()
	{
		foreach (Moon moon in moons)
		{
			moon.Angle += moon.Speed * Time.deltaTime;
			float radians = moon.Angle * Mathf.Deg2Rad;

			// Point on a flattened, tilted ellipse; the upper half of it is behind the planet
			Vector2 onOrbit = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians) * moon.Flatten) * radius * moon.OrbitRadius;
			Vector2 position = Quaternion.Euler(0f, 0f, moon.Tilt) * onOrbit;
			bool behind = Mathf.Sin(radians) > 0f;

			// A little smaller and darker when further away
			float depth = 1f - 0.15f * Mathf.Sin(radians);
			moon.Renderer.transform.localPosition = new Vector3(position.x, position.y, 0f);
			moon.Renderer.transform.localScale = Vector3.one * radius * 2f * moon.Size * depth / moonSprite.bounds.size.x;
			moon.Renderer.sortingOrder = behind ? source.sortingOrder - 1 : source.sortingOrder + 1;
		}
	}

	private void UpdateRing()
	{
		if (ringBack == null)
		{
			return;
		}

		float scale = radius * 2f * 1.9f / ringBackSprite.bounds.size.x;
		ringBack.transform.localScale = Vector3.one * scale;
		ringFront.transform.localScale = Vector3.one * scale;
		ringBack.sortingOrder = source.sortingOrder - 1;
		ringFront.sortingOrder = source.sortingOrder + 1;
	}

	private void SetDecorVisible(bool visible, bool owned)
	{
		if (!visible)
		{
			glow.enabled = false;
		}

		foreach (Moon moon in moons)
		{
			moon.Renderer.enabled = visible && owned;
		}

		if (ringBack != null)
		{
			ringBack.enabled = visible && owned;
			ringFront.enabled = visible && owned;
		}
	}

	private SpriteRenderer CreateChild(string childName, Sprite sprite)
	{
		GameObject go = new GameObject(childName, typeof(SpriteRenderer));
		go.layer = gameObject.layer;
		go.transform.SetParent(transform, false);
		SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
		renderer.sprite = sprite;
		renderer.sortingLayerID = source.sortingLayerID;
		return renderer;
	}

	private static float Lerp(System.Random random, float min, float max)
	{
		return Mathf.Lerp(min, max, (float)random.NextDouble());
	}

	// --- Generated sprites, shared by all planets ---

	private static void EnsureSprites()
	{
		if (glowSprite != null)
		{
			return;
		}

		glowSprite = ToSprite(CreateGlowTexture(64), new Rect(0, 0, 64, 64));
		moonSprite = ToSprite(CreateMoonTexture(48), new Rect(0, 0, 48, 48));

		// The ring is one texture split in two: the far half behind the planet, the near half in front
		Texture2D ring = CreateRingTexture(256, 96);
		ringBackSprite = Sprite.Create(ring, new Rect(0, 48, 256, 48), new Vector2(0.5f, 0f), 100f);
		ringFrontSprite = Sprite.Create(ring, new Rect(0, 0, 256, 48), new Vector2(0.5f, 1f), 100f);
	}

	private static Sprite ToSprite(Texture2D texture, Rect rect)
	{
		return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
	}

	private static Texture2D NewTexture(int width, int height)
	{
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		texture.wrapMode = TextureWrapMode.Clamp;
		return texture;
	}

	/// <summary>Soft halo, strongest around where the planet edge sits and fading out to the border.</summary>
	private static Texture2D CreateGlowTexture(int size)
	{
		Texture2D texture = NewTexture(size, size);
		Color[] pixels = new Color[size * size];
		float half = (size - 1) * 0.5f;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float d = new Vector2(x - half, y - half).magnitude / half;
				float a = Mathf.Clamp01((1f - d) / 0.4f);
				pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		return texture;
	}

	/// <summary>Small sphere lit from the upper left with a few soft craters.</summary>
	private static Texture2D CreateMoonTexture(int size)
	{
		Texture2D texture = NewTexture(size, size);
		Color[] pixels = new Color[size * size];
		float half = (size - 1) * 0.5f;
		Vector3 light = new Vector3(-0.5f, 0.6f, 0.62f).normalized;
		Vector2[] craters = { new Vector2(0.25f, 0.2f), new Vector2(-0.3f, -0.25f), new Vector2(0.1f, -0.45f) };
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				Vector2 p = new Vector2((x - half) / half, (y - half) / half);
				float d = p.magnitude;
				if (d > 1f)
				{
					pixels[y * size + x] = Color.clear;
					continue;
				}

				Vector3 normal = new Vector3(p.x, p.y, Mathf.Sqrt(1f - d * d));
				float shade = 0.18f + 0.82f * Mathf.Clamp01(Vector3.Dot(normal, light));
				foreach (Vector2 crater in craters)
				{
					shade *= 1f - 0.18f * Mathf.Clamp01(1f - (p - crater).magnitude / 0.18f);
				}

				// Anti-aliased edge
				float alpha = Mathf.Clamp01((1f - d) * half);
				pixels[y * size + x] = new Color(shade, shade, shade, alpha);
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		return texture;
	}

	/// <summary>Flat ellipse band with a few lighter and darker stripes, like a planetary ring.</summary>
	private static Texture2D CreateRingTexture(int width, int height)
	{
		Texture2D texture = NewTexture(width, height);
		Color[] pixels = new Color[width * height];
		float halfWidth = (width - 1) * 0.5f;
		float halfHeight = (height - 1) * 0.5f;
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float d = new Vector2((x - halfWidth) / halfWidth, (y - halfHeight) / halfHeight).magnitude;
				// Band between 72% and 98% of the ellipse, with fine stripes and soft edges
				float band = Mathf.Clamp01((d - 0.72f) / 0.03f) * Mathf.Clamp01((0.98f - d) / 0.03f);
				float stripes = 0.65f + 0.35f * Mathf.Sin(d * 90f);
				float a = band * stripes * 0.7f;
				pixels[y * width + x] = new Color(0.95f, 0.88f, 0.75f, a);
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		return texture;
	}
}
