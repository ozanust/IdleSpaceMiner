using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One manager row: rank dots, skills, where it works, and assign and promote buttons.</summary>
public class ManagerItem : MonoBehaviour
{
	[SerializeField] private Image[] rankDots;
	[SerializeField] private TMP_Text primaryText;
	[SerializeField] private TMP_Text secondaryText;
	[SerializeField] private TMP_Text assignmentText;
	[SerializeField] private Button actionButton;
	[SerializeField] private Button promoteButton;

	private static readonly Color DotOn = Color.white;
	private static readonly Color DotOff = new Color(1f, 1f, 1f, 0.15f);

	private ManagerData manager;
	private TMP_Text actionLabel;
	private Action<ManagerData> onAction;
	private Action<ManagerData> onPromote;

	public void Init(Action<ManagerData> onAction, Action<ManagerData> onPromote)
	{
		this.onAction = onAction;
		this.onPromote = onPromote;
		actionLabel = actionButton.GetComponentInChildren<TMP_Text>(true);
		actionButton.onClick.AddListener(() => this.onAction?.Invoke(manager));
		promoteButton.onClick.AddListener(() => this.onPromote?.Invoke(manager));
	}

	/// <param name="actionText">Action button label, or null to hide the button.</param>
	public void Set(ManagerData manager, string primary, string secondary, string assignment, string actionText, bool actionEnabled, bool canPromote)
	{
		this.manager = manager;

		for (int i = 0; i < rankDots.Length; i++)
		{
			rankDots[i].color = i < manager.Stars ? DotOn : DotOff;
		}

		primaryText.text = primary;
		secondaryText.text = secondary;
		assignmentText.text = assignment;

		actionButton.gameObject.SetActive(actionText != null);
		actionButton.interactable = actionEnabled;
		if (actionLabel != null && actionText != null)
		{
			actionLabel.text = actionText;
		}

		promoteButton.interactable = canPromote;
	}
}
