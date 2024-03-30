using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

public class Soldier : UnitView, ISoldier
{
	public override void Attack()
	{
		base.Attack();

	}

	public override void Talk()
	{
		base.Talk();

	}
}