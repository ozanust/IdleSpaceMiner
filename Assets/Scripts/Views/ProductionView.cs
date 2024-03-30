using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ProductionView : MonoBehaviour
{
    [Inject] IProductionController productionController;

    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private Button soldierButton;
    [SerializeField] private Unit unitPrefab;

    void Start()
    {
        soldierButton.onClick.AddListener(CreateSoldier);
    }

    private void CreateSoldier()
    {
        productionController.Build(UnitType.Soldier);
    }
}
