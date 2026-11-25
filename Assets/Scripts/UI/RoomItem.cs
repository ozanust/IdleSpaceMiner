using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One mothership room row: name, level, bonus now and next, and the upgrade button.</summary>
public class RoomItem : MonoBehaviour
{
	[SerializeField] private TMP_Text nameText;
	[SerializeField] private TMP_Text levelText;
	[SerializeField] private TMP_Text effectText;
	[SerializeField] private Button upgradeButton;

	private RoomType type;
	private TMP_Text upgradeLabel;

	public RoomType Type => type;

	public void Init(RoomType type, string roomName, Action<RoomType> onUpgrade)
	{
		this.type = type;
		nameText.text = roomName;
		upgradeLabel = upgradeButton.GetComponentInChildren<TMP_Text>(true);
		upgradeButton.onClick.AddListener(() => onUpgrade(this.type));
	}

	public void Set(string level, string effect, string upgradeText, bool canUpgrade)
	{
		levelText.text = level;
		effectText.text = effect;
		upgradeButton.interactable = canUpgrade;
		if (upgradeLabel != null)
		{
			upgradeLabel.text = upgradeText;
		}
	}
}
