using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

/// <summary>Window listing the active tasks, opened from the tasks button on the HUD.</summary>
public class TasksView : MonoBehaviour
{
	[Inject] SignalBus signalBus;
	[Inject] ITaskController taskController;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private Transform itemContainer;
	[SerializeField] private TaskItem itemPrototype;
	[SerializeField] private TMP_Text allCompletedText;

	private readonly List<TaskItem> items = new List<TaskItem>();
	private MenuType type = MenuType.Tasks;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		itemPrototype.gameObject.SetActive(false);
	}

	private void Start()
	{
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Subscribe<TaskUpdatedSignal>(OnTaskUpdated);
	}

	private void OnDestroy()
	{
		signalBus.Unsubscribe<MenuOpenSignal>(OnMenuOpen);
		signalBus.Unsubscribe<TaskUpdatedSignal>(OnTaskUpdated);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(!panel.activeInHierarchy);
			Refresh();
		}
	}

	private void CloseView()
	{
		panel.SetActive(false);
	}

	private void OnTaskUpdated()
	{
		if (panel.activeInHierarchy)
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		IReadOnlyList<int> activeTasks = taskController.GetActiveTasks();

		if (!IsShowing(activeTasks))
		{
			Rebuild(activeTasks);
		}

		foreach (TaskItem item in items)
		{
			item.Refresh();
		}

		allCompletedText.gameObject.SetActive(activeTasks.Count == 0);
	}

	private bool IsShowing(IReadOnlyList<int> activeTasks)
	{
		if (items.Count != activeTasks.Count)
		{
			return false;
		}

		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].TaskIndex != activeTasks[i])
			{
				return false;
			}
		}

		return true;
	}

	private void Rebuild(IReadOnlyList<int> activeTasks)
	{
		foreach (TaskItem item in items)
		{
			Destroy(item.gameObject);
		}

		items.Clear();

		foreach (int taskIndex in activeTasks)
		{
			TaskItem item = Instantiate(itemPrototype, itemContainer);
			item.gameObject.SetActive(true);
			item.Init(taskController, taskIndex);
			items.Add(item);
		}
	}
}
