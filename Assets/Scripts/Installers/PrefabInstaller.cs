using UnityEngine;
using Zenject;

public class PrefabInstaller : MonoInstaller
{
    [SerializeField]
    private InputService inputServicePrefab;
    [SerializeField]
    private GameManager gameManager;
    [SerializeField]
    private UnitPool unitPool;

    public override void InstallBindings()
    {
        Container.Bind<IInputService>().FromComponentInNewPrefab(inputServicePrefab).AsSingle();
        Container.Bind<IGameManager>().FromComponentInNewPrefab(gameManager).AsSingle();
        Container.Bind<IUnitPool>().FromComponentInNewPrefab(unitPool).AsSingle();

        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}