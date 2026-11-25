using TMPro;
using UnityEngine;

/// <summary>
/// A bottom bar tab that shows when its window is open: a glowing mark under it and a brighter label.
/// It watches the window itself, so it stays right however the window opens or closes.
/// The window and the mark are linked by Tools > UI > Apply Orbital Theme.
/// </summary>
public class NavTab : MonoBehaviour
{
	[SerializeField] private GameObject window;
	[SerializeField] private GameObject selectedMark;
	[SerializeField] private TMP_Text label;
	[SerializeField] private Color normalLabelColor = new Color(0.78f, 0.93f, 1f, 1f);
	[SerializeField] private Color selectedLabelColor = new Color(1f, 0.86f, 0.45f, 1f);

	private bool shown;
	private bool applied;

	public void Link(GameObject windowPanel, GameObject mark, TMP_Text tabLabel, Color normalColor)
	{
		window = windowPanel;
		selectedMark = mark;
		label = tabLabel;
		normalLabelColor = normalColor;
	}

	private void LateUpdate()
	{
		bool open = window != null && window.activeInHierarchy;
		if (applied && open == shown)
		{
			return;
		}

		applied = true;
		shown = open;
		if (selectedMark != null)
		{
			selectedMark.SetActive(open);
		}

		if (label != null)
		{
			label.color = open ? selectedLabelColor : normalLabelColor;
		}
	}
}
