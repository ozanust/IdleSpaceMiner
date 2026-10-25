using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Living sky behind the planets: twinkling stars that drift with a little parallax, glints that
/// flare and fade, drifting dust, breathing nebula glows, shooting stars and the odd slow comet.
/// Everything is drawn by the background camera on the BG layer, with textures generated in code.
/// Sizes are in pixels of a 1920 pixel tall screen and converted to world units at start.
/// </summary>
public class SpaceBackdropView : MonoBehaviour
{
	private const string BackgroundLayer = "BG";
	// In front of the backdrop sprite (z 300) but well behind everything else
	private const float Depth = 200f;
	private const float ReferenceHeight = 1920f;

	[Header("Stars")]
	[SerializeField] private int farStars = 55;
	[SerializeField] private int nearStars = 25;
	// Screen pixels the star layers move per world unit the main camera pans
	[SerializeField] private float farParallax = 2.5f;
	[SerializeField] private float nearParallax = 6f;

	[Header("Dust")]
	[SerializeField] private int dustMotes = 18;
	[SerializeField] private float dustParallax = 12f;

	[Header("Events, seconds between")]
	[SerializeField] private Vector2 glintInterval = new Vector2(1.2f, 3.5f);
	[SerializeField] private Vector2 shootingStarInterval = new Vector2(4f, 10f);
	[SerializeField] private Vector2 cometInterval = new Vector2(45f, 90f);

	private static readonly Color[] StarColors =
	{
		new Color(1f, 1f, 1f),
		new Color(0.75f, 0.85f, 1f),
		new Color(1f, 0.92f, 0.75f),
		new Color(0.7f, 1f, 0.95f)
	};

	private Camera mainCamera;
	private Camera backgroundCamera;
	private int layer;
	private float unitsPerPixel;
	private Vector2 halfSize;

	private Sprite dotSprite;
	private Sprite glintSprite;
	private Sprite streakSprite;
	private Sprite glowSprite;

	private readonly List<Star> stars = new List<Star>();
	private readonly List<Effect> effects = new List<Effect>();
	private readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();

	private float nextGlint;
	private float nextShootingStar;
	private float nextComet;

	private class Star
	{
		public SpriteRenderer Renderer;
		// Position in screen pixels from the centre before parallax
		public Vector2 Position;
		public Vector2 Drift;
		public float Parallax;
		public float Size;
		public float Brightness;
		public float TwinkleSpeed;
		public float Phase;
		// Stars now and then fade out and come back somewhere else
		public float Life;
		public float Age;
	}

	private class Effect
	{
		public SpriteRenderer Head;
		public SpriteRenderer Tail;
		public Vector2 Start;
		public Vector2 Velocity;
		public float Duration;
		public float Age;
		public float Size;
		public float TailLength;
		public float Spin;
		public Color Color;
		public EffectKind Kind;
	}

	private enum EffectKind
	{
		Glint,
		ShootingStar,
		Comet
	}

	/// <summary>Adds the backdrop behind the given background camera.</summary>
	public static SpaceBackdropView Create(Camera backgroundCamera)
	{
		GameObject go = new GameObject("SpaceBackdrop");
		go.transform.SetParent(backgroundCamera.transform, false);
		go.transform.localPosition = new Vector3(0f, 0f, Depth - backgroundCamera.transform.position.z);
		SpaceBackdropView view = go.AddComponent<SpaceBackdropView>();
		view.Setup(backgroundCamera);
		return view;
	}

	private void Setup(Camera backgroundCamera)
	{
		mainCamera = Camera.main;
		this.backgroundCamera = backgroundCamera;
		layer = LayerMask.NameToLayer(BackgroundLayer);
		gameObject.layer = layer;

		// How big the screen is at this depth for the background camera
		float distance = transform.localPosition.z;
		float halfHeight = distance * Mathf.Tan(backgroundCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		unitsPerPixel = halfHeight * 2f / ReferenceHeight;
		halfSize = new Vector2(ReferenceHeight * 0.5f * backgroundCamera.aspect, ReferenceHeight * 0.5f);

		dotSprite = MakeSprite(CreateDotTexture(32));
		glintSprite = MakeSprite(CreateGlintTexture(64));
		streakSprite = MakeSprite(CreateStreakTexture(128, 16), new Vector2(1f, 0.5f));
		glowSprite = MakeSprite(CreateDotTexture(64, 2.2f));

		CreateNebulaGlows();
		for (int i = 0; i < farStars; i++)
		{
			stars.Add(CreateStar(farParallax, 3.5f, 7f, 0.4f, 0.85f, true));
		}

		for (int i = 0; i < nearStars; i++)
		{
			stars.Add(CreateStar(nearParallax, 7f, 12f, 0.65f, 1f, true));
		}

		for (int i = 0; i < dustMotes; i++)
		{
			Star mote = CreateStar(dustParallax, 6f, 12f, 0.08f, 0.16f, false);
			mote.Drift = Random.insideUnitCircle * 6f;
			mote.TwinkleSpeed = 0.3f;
			stars.Add(mote);
		}

		float now = Time.time;
		nextGlint = now + Random.Range(glintInterval.x, glintInterval.y);
		nextShootingStar = now + Random.Range(2f, shootingStarInterval.y);
		nextComet = now + Random.Range(cometInterval.x * 0.5f, cometInterval.y);
	}

	private void Update()
	{
		float time = Time.time;
		float deltaTime = Time.deltaTime;
		// The screen shape can change, e.g. resizing the editor game view
		halfSize.x = ReferenceHeight * 0.5f * backgroundCamera.aspect;

		Vector2 pan = mainCamera != null ? (Vector2)mainCamera.transform.position : Vector2.zero;

		foreach (Star star in stars)
		{
			UpdateStar(star, time, deltaTime, pan);
		}

		if (time >= nextGlint)
		{
			nextGlint = time + Random.Range(glintInterval.x, glintInterval.y);
			SpawnGlint();
		}

		if (time >= nextShootingStar)
		{
			nextShootingStar = time + Random.Range(shootingStarInterval.x, shootingStarInterval.y);
			SpawnShootingStar();

			// Sometimes a second one follows the same path, like a small meteor shower
			if (Random.value < 0.25f)
			{
				SpawnShootingStar(effects[effects.Count - 1]);
			}
		}

		if (time >= nextComet)
		{
			nextComet = time + Random.Range(cometInterval.x, cometInterval.y);
			SpawnComet();
		}

		for (int i = effects.Count - 1; i >= 0; i--)
		{
			if (!UpdateEffect(effects[i], deltaTime))
			{
				Release(effects[i].Head);
				Release(effects[i].Tail);
				effects.RemoveAt(i);
			}
		}
	}

	// --- Stars and dust ---

	private Star CreateStar(float parallax, float minSize, float maxSize, float minBrightness, float maxBrightness, bool canRespawn)
	{
		Star star = new Star
		{
			Renderer = CreateRenderer(dotSprite, 1),
			Parallax = parallax,
			Life = canRespawn ? Random.Range(12f, 40f) : float.MaxValue
		};

		PlaceStar(star, minSize, maxSize, minBrightness, maxBrightness);
		// Start at different points of their lives so they don't all respawn together
		star.Age = Random.Range(0f, Mathf.Min(star.Life, 30f));
		return star;
	}

	private void PlaceStar(Star star, float minSize, float maxSize, float minBrightness, float maxBrightness)
	{
		star.Position = new Vector2(Random.Range(-halfSize.x, halfSize.x), Random.Range(-halfSize.y, halfSize.y));
		star.Size = Random.Range(minSize, maxSize);
		star.Brightness = Random.Range(minBrightness, maxBrightness);
		star.TwinkleSpeed = Random.Range(0.6f, 2.4f);
		star.Phase = Random.Range(0f, Mathf.PI * 2f);
		star.Renderer.color = StarColors[Random.Range(0, StarColors.Length)];
		star.Renderer.transform.localScale = ScaleFor(star.Renderer.sprite, star.Size);
	}

	private void UpdateStar(Star star, float time, float deltaTime, Vector2 pan)
	{
		star.Age += deltaTime;
		if (star.Age > star.Life)
		{
			star.Age = 0f;
			PlaceStar(star, star.Size * 0.8f, star.Size * 1.2f, star.Brightness * 0.8f, Mathf.Min(1f, star.Brightness * 1.2f));
		}

		star.Position += star.Drift * deltaTime;

		// Parallax against the main camera, wrapped so stars never run out at the edges
		Vector2 position = star.Position - pan * star.Parallax;
		position.x = Mathf.Repeat(position.x + halfSize.x, halfSize.x * 2f) - halfSize.x;
		position.y = Mathf.Repeat(position.y + halfSize.y, halfSize.y * 2f) - halfSize.y;
		star.Renderer.transform.localPosition = position * unitsPerPixel;

		// Twinkle, and fade in and out at the ends of the star's life
		float twinkle = 0.6f + 0.4f * Mathf.Sin(time * star.TwinkleSpeed + star.Phase);
		float lifeFade = star.Life < float.MaxValue ? Mathf.Clamp01(star.Age / 2f) * Mathf.Clamp01((star.Life - star.Age) / 2f) : 1f;
		Color color = star.Renderer.color;
		color.a = star.Brightness * twinkle * lifeFade;
		star.Renderer.color = color;
	}

	// Two big soft glows that slowly breathe, giving the nebula some life
	private void CreateNebulaGlows()
	{
		Color[] colors = { new Color(0.55f, 0.35f, 1f), new Color(0.2f, 0.9f, 0.85f) };
		foreach (Color color in colors)
		{
			Star glow = new Star
			{
				Renderer = CreateRenderer(glowSprite, 0),
				Parallax = 1f,
				Size = Random.Range(700f, 1000f),
				Brightness = 0.1f,
				TwinkleSpeed = Random.Range(0.15f, 0.3f),
				Phase = Random.Range(0f, Mathf.PI * 2f),
				Life = float.MaxValue,
				Position = new Vector2(Random.Range(-halfSize.x, halfSize.x) * 0.6f, Random.Range(-halfSize.y, halfSize.y) * 0.6f)
			};

			glow.Renderer.color = color;
			glow.Renderer.transform.localScale = ScaleFor(glowSprite, glow.Size);
			stars.Add(glow);
		}
	}

	// --- Effects ---

	private void SpawnGlint()
	{
		Effect glint = new Effect
		{
			Kind = EffectKind.Glint,
			Head = Acquire(glintSprite, 3),
			Start = new Vector2(Random.Range(-halfSize.x, halfSize.x) * 0.95f, Random.Range(-halfSize.y, halfSize.y) * 0.95f),
			Duration = Random.Range(0.8f, 1.4f),
			Size = Random.Range(28f, 56f),
			Spin = Random.Range(-40f, 40f),
			Color = StarColors[Random.Range(0, StarColors.Length)]
		};

		glint.Head.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 45f));
		effects.Add(glint);
	}

	/// <summary>A meteor streaking across, optionally following another one a little behind it.</summary>
	private void SpawnShootingStar(Effect leader = null)
	{
		Vector2 start;
		Vector2 direction;
		if (leader != null)
		{
			direction = leader.Velocity.normalized;
			start = leader.Start - direction * Random.Range(60f, 140f) + new Vector2(-direction.y, direction.x) * Random.Range(-60f, 60f);
		}
		else
		{
			// From the upper half, heading down and sideways
			start = new Vector2(Random.Range(-halfSize.x, halfSize.x), Random.Range(0.1f, 0.9f) * halfSize.y);
			float angle = Random.Range(200f, 245f) * Mathf.Deg2Rad;
			direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			if (Random.value < 0.5f)
			{
				direction.x = -direction.x;
			}
		}

		Effect meteor = new Effect
		{
			Kind = EffectKind.ShootingStar,
			Head = Acquire(dotSprite, 4),
			Tail = Acquire(streakSprite, 3),
			Start = start,
			Velocity = direction * Random.Range(900f, 1500f),
			Duration = Random.Range(0.6f, 1.1f),
			Size = Random.Range(7f, 11f),
			TailLength = Random.Range(180f, 320f),
			Color = Random.value < 0.8f ? Color.white : new Color(0.7f, 0.95f, 1f)
		};

		effects.Add(meteor);
	}

	// A slow comet with a long soft tail crossing high in the sky
	private void SpawnComet()
	{
		bool fromLeft = Random.value < 0.5f;
		Vector2 start = new Vector2((fromLeft ? -1.15f : 1.15f) * halfSize.x, Random.Range(0.3f, 0.8f) * halfSize.y);
		Vector2 end = new Vector2(-start.x, start.y - Random.Range(0.2f, 0.5f) * halfSize.y);
		float duration = Random.Range(14f, 20f);

		Effect comet = new Effect
		{
			Kind = EffectKind.Comet,
			Head = Acquire(glowSprite, 4),
			Tail = Acquire(streakSprite, 3),
			Start = start,
			Velocity = (end - start) / duration,
			Duration = duration,
			Size = 26f,
			TailLength = Random.Range(380f, 520f),
			Color = new Color(0.8f, 0.95f, 1f)
		};

		effects.Add(comet);
	}

	/// <summary>Moves and fades an effect, false once it has finished.</summary>
	private bool UpdateEffect(Effect effect, float deltaTime)
	{
		effect.Age += deltaTime;
		float t = effect.Age / effect.Duration;
		if (t >= 1f)
		{
			return false;
		}

		Vector2 position = effect.Start + effect.Velocity * effect.Age;
		Color color = effect.Color;

		switch (effect.Kind)
		{
			case EffectKind.Glint:
			{
				// Flares up quickly, then shrinks away while turning
				float flare = Mathf.Sin(t * Mathf.PI);
				effect.Head.transform.localScale = ScaleFor(effect.Head.sprite, effect.Size * (0.3f + 0.9f * flare));
				effect.Head.transform.localRotation *= Quaternion.Euler(0f, 0f, effect.Spin * deltaTime);
				color.a = flare;
				break;
			}

			case EffectKind.ShootingStar:
			case EffectKind.Comet:
			{
				// Fade in fast and out slowly, the comet more gently
				float fadeIn = effect.Kind == EffectKind.Comet ? 0.15f : 0.1f;
				color.a = Mathf.Clamp01(t / fadeIn) * Mathf.Clamp01((1f - t) / 0.4f);
				if (effect.Kind == EffectKind.Comet)
				{
					color.a *= 0.85f;
				}

				float tailLength = effect.TailLength * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(t * 3f));
				float angle = Mathf.Atan2(effect.Velocity.y, effect.Velocity.x) * Mathf.Rad2Deg;
				effect.Tail.transform.localPosition = position * unitsPerPixel;
				effect.Tail.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
				// The streak sprite has its pivot at the bright head end, stretch it backwards
				Rect streak = effect.Tail.sprite.rect;
				effect.Tail.transform.localScale = new Vector3(tailLength / streak.width, effect.Size * 0.6f / streak.height, 1f) * unitsPerPixel;
				Color tailColor = color;
				tailColor.a *= 0.8f;
				effect.Tail.color = tailColor;

				effect.Head.transform.localScale = ScaleFor(effect.Head.sprite, effect.Size);
				break;
			}
		}

		effect.Head.transform.localPosition = position * unitsPerPixel;
		effect.Head.color = color;
		return true;
	}

	// --- Renderers ---

	// Scale that shows a generated sprite at the given size in screen pixels
	private Vector3 ScaleFor(Sprite sprite, float pixels)
	{
		return Vector3.one * (pixels * unitsPerPixel / sprite.rect.width);
	}

	private SpriteRenderer CreateRenderer(Sprite sprite, int order)
	{
		GameObject go = new GameObject("BackdropSprite", typeof(SpriteRenderer));
		go.layer = layer;
		go.transform.SetParent(transform, false);
		SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
		renderer.sprite = sprite;
		// Above the backdrop sprite at -1
		renderer.sortingOrder = order;
		return renderer;
	}

	private SpriteRenderer Acquire(Sprite sprite, int order)
	{
		SpriteRenderer renderer = pool.Count > 0 ? pool.Pop() : CreateRenderer(sprite, order);
		renderer.sprite = sprite;
		renderer.sortingOrder = order;
		renderer.transform.localRotation = Quaternion.identity;
		renderer.color = Color.clear;
		renderer.gameObject.SetActive(true);
		return renderer;
	}

	private void Release(SpriteRenderer renderer)
	{
		if (renderer != null)
		{
			renderer.gameObject.SetActive(false);
			pool.Push(renderer);
		}
	}

	// --- Generated textures ---

	private static Sprite MakeSprite(Texture2D texture)
	{
		return MakeSprite(texture, new Vector2(0.5f, 0.5f));
	}

	// One pixel is one unit, the renderers scale them to size
	private static Sprite MakeSprite(Texture2D texture, Vector2 pivot)
	{
		return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, 1f);
	}

	private static Texture2D NewTexture(int width, int height)
	{
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;
		return texture;
	}

	/// <summary>White dot with a bright core fading softly to the edge.</summary>
	private static Texture2D CreateDotTexture(int size, float falloff = 4f)
	{
		Texture2D texture = NewTexture(size, size);
		Color[] pixels = new Color[size * size];
		float half = (size - 1) * 0.5f;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float d = new Vector2((x - half) / half, (y - half) / half).magnitude;
				float a = Mathf.Clamp01(Mathf.Exp(-d * d * falloff) - Mathf.Exp(-falloff));
				pixels[y * size + x] = new Color(1f, 1f, 1f, a);
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		return texture;
	}

	/// <summary>Four pointed star: thin bright rays crossing over a soft core.</summary>
	private static Texture2D CreateGlintTexture(int size)
	{
		Texture2D texture = NewTexture(size, size);
		Color[] pixels = new Color[size * size];
		float half = (size - 1) * 0.5f;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dx = Mathf.Abs(x - half) / half;
				float dy = Mathf.Abs(y - half) / half;
				float rays = Mathf.Max(Mathf.Exp(-dy * 40f) * (1f - dx), Mathf.Exp(-dx * 40f) * (1f - dy));
				float core = Mathf.Exp(-(dx * dx + dy * dy) * 30f);
				pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(rays + core));
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		return texture;
	}

	/// <summary>Meteor tail: bright at the right end, fading to nothing on the left, soft across.</summary>
	private static Texture2D CreateStreakTexture(int width, int height)
	{
		Texture2D texture = NewTexture(width, height);
		Color[] pixels = new Color[width * height];
		float half = (height - 1) * 0.5f;
		for (int y = 0; y < height; y++)
		{
			float across = Mathf.Exp(-Mathf.Pow((y - half) / half, 2f) * 3f);
			for (int x = 0; x < width; x++)
			{
				float along = Mathf.Pow((float)x / (width - 1), 2.2f);
				pixels[y * width + x] = new Color(1f, 1f, 1f, along * across);
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		return texture;
	}
}
