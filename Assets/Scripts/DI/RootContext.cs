using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class RootContext : MonoBehaviour
{
	[Inject] IGameManager gameManager;
	[Inject] IInputService inputService;
}
