using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class UnitController : MonoBehaviour
{
	[Inject] private IInputService inputService;

	private UnitView selectedUnit;
	private Camera playerCam;
	private RaycastHit hit;

	private void Start()
	{
		playerCam = Camera.main;

		inputService.OnMouseUp += OnSelection;
		inputService.OnRightMouseUp += OnAction;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Delete))
		{
			if (selectedUnit != null)
			{
				selectedUnit.Die();
			}
		}
	}

	private void OnSelection()
	{
		Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out hit, 1000))
		{
			if(hit.collider.gameObject.tag == "Unit")
			{
				if (selectedUnit != null)
					selectedUnit.Select(false);

				selectedUnit = hit.collider.gameObject.GetComponent<UnitView>();
				selectedUnit.Select(true);
			}
			else
			{
				if(selectedUnit != null)
				{
					selectedUnit.Select(false);
					selectedUnit = null;
				}
			}
		}
	}

	private void OnAction()
	{
		if (selectedUnit == null)
			return;

		Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out hit, 1000))
		{
			if (hit.collider.gameObject.tag == "Ground")
			{
				selectedUnit.GoTo(hit.point);
			}
		}
	}
}
