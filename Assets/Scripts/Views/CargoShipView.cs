using UnityEngine;
using Zenject;

public class CargoShipView : MonoBehaviour
{
	// Converts ship speed stat (planet settings units) to world units per second
	private const float WorldSpeedScale = 10f;

	SignalBus signalBus;

	private float shipSpeed;
	private int cargoSize;
	private int targetPlanetId;
	private Vector3 currentTarget;
	// turn this into vector3 if transform is not useful for tween functions
	private Transform targetPlanet;
	private Vector3 mothershipDefaultPosition = new Vector3(0, 0, 0);

	[Header("Trail")]
	[SerializeField] private float trailTime = 0.3f;
	[SerializeField] private float trailWidth = 0.06f;
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

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.tag == "Planet")
		{
			currentTarget = mothershipDefaultPosition;
			signalBus.Fire(new CargoShipPlanetArrivalSignal() { PlanetId = targetPlanetId, CargoCapacity = cargoSize });
		}
		else if (collision.gameObject.tag == "Mothership")
		{
			currentTarget = targetPlanet.position;
			signalBus.Fire(new CargoShipMothershipArrivalSignal() { PlanetId = targetPlanetId });
		}
	}

	private void Update()
	{
		Vector3 direction = (currentTarget - transform.position).normalized;

		transform.position += direction * shipSpeed * Time.deltaTime * WorldSpeedScale;

		float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
		Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

		transform.rotation = Quaternion.RotateTowards(
			transform.rotation,
			targetRotation,
			180f * Time.deltaTime * 10 // turn speed
		);
	}

	private void OnPlanetUpdated(PlanetUpdatedSignal signal)
	{
		if (signal.PlanetId != targetPlanetId)
		{
			return;
		}


	}
}
