using UnityEngine;
using Zenject;

public class PrefabInstaller : MonoInstaller
{
    [SerializeField]
    private InputService inputServicePrefab;
    [SerializeField]
    private GameManager gameManager;

    public override void InstallBindings()
    {
        Container.Bind<IInputService>().FromComponentInNewPrefab(inputServicePrefab).AsSingle();
        Container.Bind<IGameManager>().FromComponentInNewPrefab(gameManager).AsSingle();

        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}