using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class MyCameraController : MonoBehaviour
{
    [SerializeField] InputActionReference moveCameraAction, zoomAction, shiftCameraSpeedAction, cursorAction, selectAction;
	[SerializeField] GameObject pointer;
	[SerializeField] MyCharacterController[] charControllers;
	[SerializeField] float maxClickDistance = 100f;
	[SerializeField] LayerMask layerMask;
	[SerializeField] DecalProjector goalMarker;
	[SerializeField] [Min(1f)] float cameraSpeed = 20f;
	[SerializeField] [Min(2f)] float cameraShiftSpeedFactor = 2f;
	[SerializeField] [Min(0f)] float smoothTime = 0.25f, gamepadPointerSpeed = 1f;
	[SerializeField] float zoomSpeed = 100f, minHeight = 10f;
	[SerializeField] [Min(10f)] float maxHeight = 30f;
	[SerializeField] [Min(10f)] float maxDistance = 70f;
	[SerializeField] PlayerInput playerInput;

	private Vector3 velocity;
	private Vector2 moveCamera, cursorPosition, pointerPosition;
	private float zoom;
	private float shiftMoveSpeed;
	private bool gamepadController;
	private MyCharacterController selectedPlayer;

	const string nameMouseWheel = "Mouse ScrollWheel", keyboardScheme = "Keyboard", gamepadScheme = "Gamepad";

	public enum Fade
	{
		In, Out
	}

	public static MyCameraController Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
			Destroy(this);
		
		else
		{
			Instance = this;
		}

		moveCameraAction.action.started += OnMove;
		moveCameraAction.action.performed += OnMove;
		moveCameraAction.action.canceled += OnMove;

		zoomAction.action.started += Zoom;
		zoomAction.action.performed += Zoom;
		zoomAction.action.canceled += Zoom;

		shiftCameraSpeedAction.action.performed += ShiftMoveSpeed;
		shiftCameraSpeedAction.action.canceled += ShiftMoveSpeed;

		cursorAction.action.started += CursorMove;
		cursorAction.action.performed += CursorMove;
		cursorAction.action.canceled += CursorMove;

		selectAction.action.started += Select;

		playerInput.onControlsChanged += OnControlsChanged;
	}

	private void OnEnable()
	{
		selectedPlayer = charControllers[0];
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
		moveCameraAction.action.started -= OnMove;
		moveCameraAction.action.performed -= OnMove;
		moveCameraAction.action.canceled -= OnMove;

		zoomAction.action.started -= Zoom;
		zoomAction.action.performed += Zoom;
		zoomAction.action.canceled += Zoom;

		shiftCameraSpeedAction.action.performed -= ShiftMoveSpeed;
		shiftCameraSpeedAction.action.canceled -= ShiftMoveSpeed;

		cursorAction.action.started -= CursorMove;
		cursorAction.action.performed -= CursorMove;
		cursorAction.action.canceled -= CursorMove;

		selectAction.action.started -= Select;

		playerInput.onControlsChanged -= OnControlsChanged;
	}

	void Update()
	{
		if (moveCamera.sqrMagnitude > 0) MoveCamera(moveCamera);

		// Zoom
		if (!Mathf.Approximately(zoom, 0))
		{
			var mouseWheel = Time.fixedDeltaTime * zoomSpeed * zoom;
			var groundHit = GetWorldGround(Camera.main.transform, minHeight, layerMask, out Vector3 groundHitPoint);
			//var desiredHeight = transform.position + transform.forward * mouseWheel;
			//var newPosition = Vector3.SmoothDamp(transform.position, desiredHeight, ref velocity, smoothTime);
			var newPosition = transform.position + transform.forward * mouseWheel;
			float newDistance = Vector2.Distance(newPosition, groundHit ? groundHitPoint : selectedPlayer.transform.position);
			if (newDistance <= maxHeight && newDistance >= minHeight)
				transform.position = newPosition;
		}

		// Limit by ground
		var ground = GetWorldGround(Camera.main.transform, minHeight, layerMask, out Vector3 groundPoint);
		if (ground)
		{
			var desiredHeight = new Vector3(transform.position.x, groundPoint.y + minHeight, transform.position.z);
			var newPosition = Vector3.SmoothDamp(transform.position, desiredHeight, ref velocity, smoothTime);
			if (transform.position.y - groundPoint.y <= minHeight)
			{
				transform.position = newPosition;
			}	
		}
		if (gamepadController && cursorPosition.magnitude > 0)
		{ 
			pointer.transform.Translate(Time.deltaTime * gamepadPointerSpeed * cursorPosition);
			pointerPosition = pointer.transform.position;
		}
	}

	private void Select(InputAction.CallbackContext context)
	{
		bool gotPoint = GetWorldPoint(Camera.main, gamepadController ? pointerPosition : cursorPosition, maxClickDistance, layerMask, out Vector3 clickPoint);
		if (gotPoint)
		{
			selectedPlayer.SetOff(clickPoint);
			goalMarker.transform.position = clickPoint;
			ShowMarker();
			Debug.Log($"Goal: {clickPoint}");
		}
		else
		{
			HideMarker();
			Debug.Log("Nothing");
		}
	}

	private void OnControlsChanged(PlayerInput input)
    {
        Debug.Log("Control Scheme Changed: " + input.currentControlScheme);
		switch (input.currentControlScheme)
		{
			case gamepadScheme: gamepadController = true; break;
			default: gamepadController = false; break;
		}
		pointer.SetActive(gamepadController);
    }

	private void OnMove(InputAction.CallbackContext context)
    {
        moveCamera = context.ReadValue<Vector2>();
    }

	private void CursorMove(InputAction.CallbackContext context)
    {
        cursorPosition = context.ReadValue<Vector2>();
		//Debug.Log($"CursorMove {cursorPosition}");
    }
	
	private void Zoom(InputAction.CallbackContext context)
	{
		zoom = Mathf.Clamp(context.ReadValue<Vector2>().y, -1, 1);
	}

	private void ShiftMoveSpeed(InputAction.CallbackContext context)
    {
        shiftMoveSpeed = context.ReadValue<float>();
    }

	void MoveCamera(Vector3 direction)
	{
		var forwardOnGround = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
		var speed = shiftMoveSpeed > 0 ? cameraSpeed * cameraShiftSpeedFactor : cameraSpeed;
        var forwardMovement = forwardOnGround * direction.y;
        var horizontalMovement = transform.right * direction.x;
		// Combine movements and scale by speed
        Vector3 movement = speed * Time.deltaTime * (forwardMovement + horizontalMovement);

        // Apply movement to the camera
        transform.position += movement;

		// Clamp camera position within radius
        var offset = transform.position - selectedPlayer.transform.position;
        if (offset.magnitude > maxDistance)
        {
            // Clamp the offset to the maximum radius
            offset = offset.normalized * maxDistance;

            // Update camera position
            transform.position = selectedPlayer.transform.position + offset;
        }
	}

	public static bool GetWorldPoint(Camera camera, Vector2 screenPosition, float distance, LayerMask mask, out Vector3 worldPosition)
	{
		var ray = camera.ScreenPointToRay(screenPosition);
		worldPosition = Vector3.zero;
		var raycast = Physics.Raycast(ray, out var hit, distance, mask, QueryTriggerInteraction.UseGlobal);
		if (raycast)
		{
			worldPosition = hit.point;
			Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red);
			return true;
		}
		return false;
	}
	public static bool GetWorldGround(Transform origin, float distance, LayerMask mask, out Vector3 worldPosition)
	{
		var position = origin.position;
		var drawRay = new Ray(position, Vector3.down);
		// How high above the ground it might be
		var originOffest = Vector3.up * 100;
		var ray = new Ray(position + originOffest, Vector3.down);
		worldPosition = Vector3.zero;
		var raycast = Physics.Raycast(ray, out var hit, distance + originOffest.y, mask, QueryTriggerInteraction.UseGlobal);
		if (raycast)
		{
			worldPosition = hit.point;
			Debug.DrawRay(drawRay.origin, drawRay.direction * hit.distance + originOffest, Color.green);
			return true;
		}
		return false;
	}

	public void ShowMarker()
	{
		StopAllCoroutines();
		StartCoroutine(MarkerOpacity(Fade.In));
	}
	public void HideMarker()
	{
		StopAllCoroutines();
		StartCoroutine(MarkerOpacity(Fade.Out));
	}

	IEnumerator MarkerOpacity(Fade fade)
	{
		var timeElapsed = 0f;
		var lerpDuration = 0.35f;
		float startFade = fade == Fade.In ? 0 : 1;
		float endFade = fade == Fade.In ? 1 : 0;
		float valueFade;
		while (timeElapsed < lerpDuration)
		{
			valueFade = Mathf.Lerp(startFade, endFade, timeElapsed / lerpDuration);
			timeElapsed += Time.deltaTime;
			goalMarker.fadeFactor = valueFade;
			yield return null;
		}
		valueFade = endFade;
		goalMarker.fadeFactor = valueFade;
	}
}
