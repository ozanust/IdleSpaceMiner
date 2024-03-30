using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUnitPool
{
	UnitView GetUnit(Unit unit);
	UnitView GetUnit(Unit unit, Vector3 spawnPosition);
}

public class UnitPool : MonoBehaviour, IUnitPool
{
	[SerializeField]
	private UnitView[] unitPrototypesArray;

	private Stack<UnitView> unitPoolStack = new Stack<UnitView>();
	private Dictionary<UnitType, UnitView> unitsDict = new Dictionary<UnitType, UnitView>();

	private void Awake()
	{
		foreach(UnitView uv in unitPrototypesArray)
		{
			unitsDict.Add(uv.Type, uv);
		}
	}

	public UnitView GetUnit(Unit unit)
	{
		UnitView tempUnit;
		if (unitPoolStack.Count == 0)
		{
			tempUnit = Instantiate(unitsDict[unit.Type]);
			tempUnit.OnDie.AddListener(OnUnitDies);
			return tempUnit;
		}

		tempUnit = unitPoolStack.Pop();
		tempUnit.SetUnit(unit);
		tempUnit.gameObject.SetActive(true);

		return tempUnit;
	}

	public UnitView GetUnit(Unit unit, Vector3 spawnPosition)
	{
		UnitView tempUnit;
		if (unitPoolStack.Count == 0)
		{
			tempUnit = Instantiate(unitsDict[unit.Type], spawnPosition, Quaternion.identity);
			tempUnit.OnDie.AddListener(OnUnitDies);
			return tempUnit;
		}

		tempUnit = unitPoolStack.Pop();
		tempUnit.SetUnit(unit);
		tempUnit.gameObject.transform.position = spawnPosition;
		tempUnit.gameObject.SetActive(true);

		return tempUnit;
	}

	private void OnUnitDies(UnitView unit)
	{
		unitPoolStack.Push(unit);
	}
}
