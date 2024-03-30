using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using TMPro;

public class ResourcesView : MonoBehaviour
{
    [Inject] IPlayerModel playerModel;
    [Inject] SignalBus signalBus;
	[Inject] ResourceSettings resourceSettings;

	[SerializeField] private ResourceInfoItem infoItemPrototype;

	[SerializeField] private VerticalLayoutGroup oreResourcesLayout;
	[SerializeField] private VerticalLayoutGroup alloyResourcesLayout;
	[SerializeField] private VerticalLayoutGroup itemResourcesLayout;

	[SerializeField] private Button oreButton;
	[SerializeField] private Button alloyButton;
	[SerializeField] private Button itemButton;

	[SerializeField] private GameObject panel;
	[SerializeField] private Button closeButton;
	[SerializeField] private TMP_Text panelTitle;

	private Dictionary<ResourceType, ResourceInfoItem> infoItems = new Dictionary<ResourceType, ResourceInfoItem>();
	private MenuType type = MenuType.Resources;
	private ResourceType currentResourceSelection;

	private void Awake()
	{
		closeButton.onClick.AddListener(CloseView);
		oreButton.onClick.AddListener(OnOreSelected);
		alloyButton.onClick.AddListener(OnAlloySelected);
		itemButton.onClick.AddListener(OnItemSelected);
	}

	private void Start()
	{
		signalBus.Subscribe<ResourcesViewUpdatedSignal>(OnResourcesUpdated);
		signalBus.Subscribe<ResourcesViewInitializedSignal>(OnResourcesInitialized);
		signalBus.Subscribe<MenuOpenSignal>(OnMenuOpen);
	}

	private void OnMenuOpen(MenuOpenSignal signal)
	{
		if (signal.Type == type)
		{
			panel.SetActive(true);
		}
	}

	private void OnResourcesInitialized(ResourcesViewInitializedSignal signal)
	{
		foreach(ResourceType rt in signal.ResourcesToInitialize.Keys)
		{
			if (!infoItems.ContainsKey(rt))
			{
				MainResourceType mainType = resourceSettings.GetResourceParentType(rt);
				ResourceInfoItem infoItem = Instantiate(infoItemPrototype, GetResourceParent(mainType));
				ResourceDataSetting resourceData = resourceSettings.GetResourceData(rt);
				infoItem.InitializeResource(resourceData.Icon, resourceData.Name);

				infoItems.Add(rt, infoItem);
			}
		}
	}

	private void OnResourcesUpdated(ResourcesViewUpdatedSignal signal)
	{
		foreach (ResourceType rt in signal.ResourcesToUpdate.Keys)
		{
			if (!infoItems.ContainsKey(rt))
			{
				MainResourceType mainType = resourceSettings.GetResourceParentType(rt);
				ResourceInfoItem infoItem = Instantiate(infoItemPrototype, GetResourceParent(mainType));
				ResourceDataSetting resourceData = resourceSettings.GetResourceData(rt);
				infoItem.InitializeResource(resourceData.Icon, resourceData.Name);
				infoItem.UpdateAmount(playerModel.GetResource(rt), playerModel.GetResource(rt) * resourceSettings.GetResourceValue(rt));
				infoItem.SetSelectionButtonAction(() => OnSelectResourceItem(rt));

				infoItems.Add(rt, infoItem);
			}
			else
			{
				infoItems[rt].UpdateAmount(playerModel.GetResource(rt), playerModel.GetResource(rt) * resourceSettings.GetResourceValue(rt));
			}
		}
	}

	private void OnSelectResourceItem(ResourceType type)
	{
		if (type != currentResourceSelection)
		{
			signalBus.Fire(new ResourceDeselectedToSellSignal() { Type = currentResourceSelection });
			currentResourceSelection = type;
		}

		// Firing this anyway to re-set selection if there are some issues
		signalBus.Fire(new ResourceSelectedToSellSignal() { Type = type });
	}

	private void CloseView()
	{
		panel.SetActive(false);
		signalBus.Fire<ResourcesViewClosedSignal>();
	}

	private void OnOreSelected()
	{
		ResourcePanelSelection(true, false, false);
	}

	private void OnAlloySelected()
	{
		ResourcePanelSelection(false, true, false);
	}

	private void OnItemSelected()
	{
		ResourcePanelSelection(false, false, true);
	}

	private void ResourcePanelSelection(bool isOre, bool isAlloy, bool isItem)
	{
		oreResourcesLayout.gameObject.SetActive(isOre);
		alloyResourcesLayout.gameObject.SetActive(isAlloy);
		itemResourcesLayout.gameObject.SetActive(isItem);
		panelTitle.text = isOre ? "Ores" : isAlloy ? "Alloys" : isItem ? "Items" : "";
		signalBus.Fire(new ResourceDeselectedToSellSignal() { Type = currentResourceSelection });
	}

	// TODO: Refactor, not good
	private Transform GetResourceParent(MainResourceType type)
	{
		if (type == MainResourceType.Ore)
		{
			return oreResourcesLayout.transform;
		}

		if (type == MainResourceType.Alloy)
		{
			return alloyResourcesLayout.transform;
		}

		if (type == MainResourceType.Item)
		{
			return itemResourcesLayout.transform;
		}

		Debug.LogError("Resource parent couldn't found, returning null!");
		return null;
	}
}
