using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputService : MonoBehaviour, IInputService
{
	Action onMouseUp;
	Action onRightMouseUp;
	Action onMouseHeld;
	Action<float> mouseHeldDuration;
	Action<float> scroll;

	public Action OnMouseUp { get { return onMouseUp; } set { onMouseUp = value; }  }
	public Action OnRightMouseUp { get { return onRightMouseUp; } set { onRightMouseUp = value; }  }
	public Action OnMouseHeld { get { return onMouseHeld; } set { onMouseHeld = value; }  }
	public Action<float> MouseHeldDuration { get { return mouseHeldDuration; } set { mouseHeldDuration = value; }  }
	public Action<float> OnScroll { get { return scroll; } set { scroll = value; } }

	private float mouseHeldTimer = 0f;
	private float scrollInput = 0f;

	void Update()
    {
		if (Input.GetMouseButtonUp(0))
		{
			onMouseUp?.Invoke();
			if(mouseHeldTimer > Constants.EPSILON)
			{
				mouseHeldDuration?.Invoke(mouseHeldTimer);
				mouseHeldTimer = 0;
			}
		}

		if (Input.GetMouseButtonUp(1))
		{
			onRightMouseUp?.Invoke();
		}

		if (Input.GetMouseButton(0))
		{
			mouseHeldTimer += Time.deltaTime;
			onMouseHeld?.Invoke();
		}

		scrollInput = Input.mouseScrollDelta.y;

		if (scrollInput > 0 || scrollInput < 0)
		{
			scroll?.Invoke(scrollInput);
		}
	}
}
