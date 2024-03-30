using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

public class UnitView : MonoBehaviour, IUnit
{
	[SerializeField]
	private NavMeshAgent navAgent;
	[SerializeField]
	private Image healthFillImage;
	[SerializeField]
	private GameObject selectionIndicator;
	[SerializeField]
	private float unitSpeed;
	[SerializeField]
	private UnitType unitType;

	private Camera playerCam;
	protected bool isActive;
	protected float health;
	protected int attackPower;
	protected float attackSpeed;
	protected int level;

	public UnityEvent<UnitView> OnDie;

	public NavMeshAgent NavAgent => navAgent;
	public bool IsActive => isActive;
	public UnitType Type => unitType;

	public Action OnDestinationReached;

	private void Start()
	{
		playerCam = Camera.main;
	}

	public void GoTo(Vector3 destination)
	{
		navAgent.SetDestination(destination);
		navAgent.speed = unitSpeed;
	}

	public void Select(bool isSelected)
	{
		selectionIndicator.SetActive(isSelected);
	}

	public virtual void Attack() { }
	public virtual void Talk() { }

	public void TakeDamage(float damageAmount)
	{
		health -= damageAmount;
		healthFillImage.fillAmount = health / 100f;

		if (health <= 0)
		{
			Die();
		}
	}

	public void Die()
	{
		OnDie?.Invoke(this);
		gameObject.SetActive(false);
		isActive = false;
	}

	public void SetUnit(Unit unit)
	{
		health = unit.Health;
		attackPower = unit.AttackPower;
		attackSpeed = unit.AttackSpeed;
		level = unit.Level;
		unitType = unit.Type;
	}

	private void LateUpdate()
	{
		if (selectionIndicator.activeInHierarchy)
			selectionIndicator.transform.LookAt(playerCam.transform);
	}

	private void FixedUpdate()
	{
		if (navAgent.speed > 0)
		{
			if (Vector3.Distance(transform.position, navAgent.destination) < 0.01f)
			{
				OnDestinationReached?.Invoke();
			}
		}
	}
}
