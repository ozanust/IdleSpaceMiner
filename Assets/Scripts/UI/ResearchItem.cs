using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ResearchItem : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private Button button;

    [SerializeField] ResearchType type;

	private void Awake()
	{
		button.onClick.AddListener(OnClickButton);
	}

	private void Start()
	{
		signalBus.Subscribe<ResearchCompletedSignal>(OnResearchCompleted);
	}

	private void OnResearchCompleted(ResearchCompletedSignal signal)
	{
		if (signal.ResearchType == type)
		{
			borderImage.color = Color.green;
		}
	}

	private void OnClickButton()
	{
		signalBus.Fire(new ResearchInfoOpenSignal() { Type = type });
	}
}
