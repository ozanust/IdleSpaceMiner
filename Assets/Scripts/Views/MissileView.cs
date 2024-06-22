using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileView : MonoBehaviour
{
    private Vector3 target;
    private float missileSpeed = 5;

	public void Init(Vector3 target)
	{
		this.target = target;
	}

	private void Update()
	{
		this.transform.position += (target - transform.position).normalized * missileSpeed * Time.deltaTime * 5;
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
