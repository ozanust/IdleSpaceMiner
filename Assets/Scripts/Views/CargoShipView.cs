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

	// Direction of travel in degrees. The ship always flies forward and steers towards its target.
	private float heading;
	private bool hasHeading;

	[Header("Movement")]
	[Tooltip("Radius of the arc the ship flies when turning, in world units. Keep it below the planet size.")]
	[SerializeField] private float turnRadius = 1.2f;
	[Tooltip("Share of full speed while facing away from the target, so ships ease through U-turns.")]
	[SerializeField, Range(0.1f, 1f)] private float turningSpeedFactor = 0.5f;

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
		hasHeading = false;
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
		Vector3 toTarget = currentTarget - transform.position;
		float desiredHeading = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
		if (!hasHeading)
		{
			heading = desiredHeading;
			hasHeading = true;
		}

		// Slow down while facing away from the target, back to full speed once lined up
		float alignment = Mathf.Cos(Mathf.DeltaAngle(heading, desiredHeading) * Mathf.Deg2Rad);
		float speed = shipSpeed * WorldSpeedScale * Mathf.Lerp(turningSpeedFactor, 1f, (alignment + 1f) * 0.5f);

		// Turn rate follows speed, so the turning arc has the same radius at any ship speed
		float turnRate = speed / turnRadius * Mathf.Rad2Deg;
		heading = Mathf.MoveTowardsAngle(heading, desiredHeading, turnRate * Time.deltaTime);

		Vector3 previous = transform.position;
		float radians = heading * Mathf.Deg2Rad;
		transform.position += new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * speed * Time.deltaTime;
		transform.rotation = Quaternion.Euler(0f, 0f, heading - 90f);

		CheckArrival(previous);
	}

	/// <summary>
	/// Arrival is measured by distance instead of trigger events: the ship has no rigidbody and
	/// sleeping or missing planet rigidbodies stopped triggers firing, leaving ships stuck on a planet.
	/// </summary>
	private void CheckArrival(Vector3 previous)
	{
		if (targetPlanet == null)
		{
			return;
		}

		// Measured along this frame's whole move, so very fast ships can't skip past a target
		if (!returning)
		{
			if (DistanceToSegment(targetPlanet.position, previous, transform.position) <= GetArrivalRadius(targetPlanet))
			{
				returning = true;
				currentTarget = mothershipDefaultPosition;
				signalBus.Fire(new CargoShipPlanetArrivalSignal() { PlanetId = targetPlanetId, CargoCapacity = cargoSize });
			}
		}
		else if (DistanceToSegment(mothershipDefaultPosition, previous, transform.position) <= MothershipArrivalRadius)
		{
			returning = false;
			currentTarget = targetPlanet.position;
			signalBus.Fire(new CargoShipMothershipArrivalSignal() { PlanetId = targetPlanetId });
		}
	}

	private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
	{
		Vector3 segment = end - start;
		float lengthSquared = segment.sqrMagnitude;
		float t = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared) : 0f;
		return Vector3.Distance(point, start + segment * t);
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
