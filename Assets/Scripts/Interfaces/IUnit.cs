using UnityEngine;

public interface IUnit
{
	public void GoTo(Vector3 destination);
	public void Select(bool isSelected);
}
