using UnityEngine;
using Zenject;
using TMPro;

/// <summary>Short message at the top of the screen: a failed purchase in red, an ad reward in gold.</summary>
public class ToastView : MonoBehaviour
{
	private const float ShowTime = 1.6f;
	private const float FadeTime = 0.4f;

	[Inject] SignalBus signalBus;

	[SerializeField] private CanvasGroup group;
	[SerializeField] private TMP_Text messageText;

	private static readonly Color RewardColor = new Color(1f, 0.85f, 0.4f);
	private Color failColor;

	private float hideAt;

	private void Awake()
	{
		failColor = messageText.color;
		group.alpha = 0f;
		signalBus.Subscribe<PurchaseSignal>(OnPurchase);
		signalBus.Subscribe<RewardGrantedSignal>(OnReward);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<PurchaseSignal>(OnPurchase);
		signalBus.Unsubscribe<RewardGrantedSignal>(OnReward);
	}

	private void OnPurchase(PurchaseSignal signal)
	{
		if (!signal.Success && !string.IsNullOrEmpty(signal.FailMessage))
		{
			Show(signal.FailMessage, failColor);
		}
	}

	private void OnReward(RewardGrantedSignal signal)
	{
		Show(signal.Message, RewardColor);
	}

	public void Show(string message, Color color)
	{
		messageText.text = message;
		messageText.color = color;
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
