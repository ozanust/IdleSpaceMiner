using UnityEngine.EventSystems;

public static class PointerUtility
{
	/// <summary>True when the pointer moved far enough to be a camera drag rather than a tap.</summary>
	public static bool IsDrag(PointerEventData eventData)
	{
		float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
		return (eventData.position - eventData.pressPosition).sqrMagnitude > threshold * threshold;
	}
}
