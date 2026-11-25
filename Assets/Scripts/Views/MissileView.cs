using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileView : MonoBehaviour
{
	private Vector3 target;
	private float missileSpeed = 5;

	[Tooltip("Direction the nose points in the sprite, in degrees from the right; the missile sprite is drawn heading up and to the right")]
	[SerializeField] private float spriteNoseAngle = 45f;

	public void Init(Vector3 target)
	{
		this.target = target;
		FaceTarget();
	}

	private void Update()
	{
		this.transform.position += (target - transform.position).normalized * missileSpeed * Time.deltaTime * 5;
	}

	// Turns the nose towards the asteroid, allowing for the angle the sprite is drawn at
	private void FaceTarget()
	{
		Vector3 direction = target - transform.position;
		if (direction.sqrMagnitude < 0.0001f)
		{
			return;
		}

		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - spriteNoseAngle;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.tag == "Asteroid")
		{
			// play explosion vfx
			Destroy(this.gameObject);
		}
	}
}
