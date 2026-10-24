using UnityEngine;
using Zenject;

public class CargoShipView : MonoBehaviour
{
	// Converts ship speed stat (planet settings units) to world units per second
	private const float WorldSpeedScale = 3f;

	SignalBus signalBus;

	private float shipSpeed;
	private int cargoSize;
	private int targetPlanetId;
	private Vector3 currentTarget;
	// turn this into vector3 if transform is not useful for tween functions
	private Transform targetPlanet;
	private Vector3 mothershipDefaultPosition = new Vector3(0, 0, 0);
	private bool returning;

	// Distance from a target's centre that counts as arrived, when it has no collider to measure
	private const float DefaultArrivalRadius = 0.5f;
	private const float MothershipArrivalRadius = 3f;

	[Header("Trail")]
	[SerializeField] private float trailTime = 0.6f;
	[SerializeField] private float trailWidth = 0.12f;
	[SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.5f);
	[SerializeField] private int trailSortingOrder = -1;

	private void Awake()
	{
		SetupTrail();
	}

	/// <summary>
	/// Adds a fading trail behind the ship. A TrailRenderer already on the prefab is
	/// left as configured there, so it can be tuned in the editor instead.
	/// </summary>
	private void SetupTrail()
	{
		if (GetComponent<TrailRenderer>() != null)
		{
			return;
		}

		TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
		trail.time = trailTime;
		trail.minVertexDistance = 0.05f;
		trail.widthCurve = AnimationCurve.Linear(0f, trailWidth, 1f, 0f);
		trail.material = new Material(Shader.Find("Sprites/Default"));
		trail.sortingOrder = trailSortingOrder;
		trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		trail.receiveShadows = false;

		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
			new[] { new GradientAlphaKey(trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
		trail.colorGradient = gradient;
	}

	[Inject]
	public void Construct(SignalBus signalBus)
	{
		this.signalBus = signalBus;
		this.signalBus.Subscribe<PlanetUpdatedSignal>(OnPlanetUpdated);
	}

	public void SetTargetPlanet(Transform targetPlanet, int targetPlanetId)
	{
		currentTarget = targetPlanet.position;
		this.targetPlanet = targetPlanet;
		this.targetPlanetId = targetPlanetId;
	}

	public void SetShipSpeed(float newSpeed)
	{
		shipSpeed = newSpeed;
	}

	public void SetCargoSize(int newCargo)
	{
		cargoSize = newCargo;
	}

	private void Update()
	{
		Vector3 direction = (currentTarget - transform.position).normalized;

		// MoveTowards never overshoots, so fast ships can't skip past the target
		transform.position = Vector3.MoveTowards(transform.position, currentTarget, shipSpeed * Time.deltaTime * WorldSpeedScale);
		CheckArrival();

		float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
		Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

		transform.rotation = Quaternion.RotateTowards(
			transform.rotation,
			targetRotation,
			180f * Time.deltaTime * 10 // turn speed
		);
	}

	/// <summary>
	/// Arrival is measured by distance instead of trigger events: the ship has no rigidbody and
	/// sleeping or missing planet rigidbodies stopped triggers firing, leaving ships stuck on a planet.
	/// </summary>
	private void CheckArrival()
	{
		if (targetPlanet == null)
		{
			return;
		}

		if (!returning)
		{
			if (Vector3.Distance(transform.position, targetPlanet.position) <= GetArrivalRadius(targetPlanet))
			{
				returning = true;
				currentTarget = mothershipDefaultPosition;
				signalBus.Fire(new CargoShipPlanetArrivalSignal() { PlanetId = targetPlanetId, CargoCapacity = cargoSize });
			}
		}
		else if (Vector3.Distance(transform.position, mothershipDefaultPosition) <= MothershipArrivalRadius)
		{
			returning = false;
			currentTarget = targetPlanet.position;
			signalBus.Fire(new CargoShipMothershipArrivalSignal() { PlanetId = targetPlanetId });
		}
	}

	private static float GetArrivalRadius(Transform target)
	{
		Collider2D collider = target.GetComponent<Collider2D>();
		return collider != null ? collider.bounds.extents.x : DefaultArrivalRadius;
	}

	private void OnPlanetUpdated(PlanetUpdatedSignal signal)
	{
		if (signal.PlanetId != targetPlanetId)
		{
			return;
		}


	}
}
