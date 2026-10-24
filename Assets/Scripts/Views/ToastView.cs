using UnityEngine;
using Zenject;
using TMPro;

/// <summary>Short message at the top of the screen, e.g. when a purchase fails for lack of money.</summary>
public class ToastView : MonoBehaviour
{
	private const float ShowTime = 1.6f;
	private const float FadeTime = 0.4f;

	[Inject] SignalBus signalBus;

	[SerializeField] private CanvasGroup group;
	[SerializeField] private TMP_Text messageText;

	private float hideAt;

	private void Awake()
	{
		group.alpha = 0f;
		signalBus.Subscribe<PurchaseSignal>(OnPurchase);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<PurchaseSignal>(OnPurchase);
	}

	private void OnPurchase(PurchaseSignal signal)
	{
		if (!signal.Success && !string.IsNullOrEmpty(signal.FailMessage))
		{
			Show(signal.FailMessage);
		}
	}

	public void Show(string message)
	{
		messageText.text = message;
		hideAt = Time.unscaledTime + ShowTime;
		group.alpha = 1f;
	}

	private void Update()
	{
		if (group.alpha > 0f && Time.unscaledTime > hideAt)
		{
			group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime / FadeTime);
		}
	}
}
