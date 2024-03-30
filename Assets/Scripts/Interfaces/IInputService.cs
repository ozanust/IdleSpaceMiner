using System;

public interface IInputService
{
	Action OnMouseUp { get; set; }
	Action OnRightMouseUp { get; set; }
	Action OnMouseHeld { get; set; }
	Action<float> MouseHeldDuration { get; set; }
	Action<float> OnScroll { get; set; }
}