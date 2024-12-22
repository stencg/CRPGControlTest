using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    [SerializeField] private InputActionReference moveCameraAction;
	[SerializeField] private InputActionReference zoomAction;

	public CharController[] charControllers;
	public float maxClickDistance = 100f;
	public LayerMask layerMask;
	public DecalProjector goalMarker;
	[Min(1f)] public float cameraSpeed = 5f;
	[Min(2f)] public float cameraShiftSpeed = 5f;
	[Min(10f)] public float cameraWheelSpeed = 500f;
	[Min(0f)] public float smoothTime = 0.25f;
	[Min(10f)] public float maxHeight = 30f;
	[Min(10f)] public float maxDistance = 70f;
	public float minHeight = 10f;

	const string nameMouseWheel = "Mouse ScrollWheel";
	private Vector3 velocity;
	private Vector2 move;
	private float zoom;
	CharController selectedPlayer;

	public enum Fade
	{
		In, Out
	}

	public static CameraController Instance { get; private set; }

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
		zoomAction.action.performed += Zoom;
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
		zoomAction.action.performed -= Zoom;
	}

	void Update()
	{
		if (move.sqrMagnitude > 0) MoveCamera(move);

		// Zoom
		var mouseWheel = Input.GetAxis(nameMouseWheel);
		if (!Mathf.Approximately(mouseWheel, 0))
		{
			mouseWheel *= Time.fixedDeltaTime * cameraWheelSpeed;
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
			//var desiredHeight = new Vector3(transform.position.x, groundPoint.y + minHeight, transform.position.z);
			//var newPosition = Vector3.SmoothDamp(transform.position, desiredHeight, ref velocity, smoothTime);
			var newPosition = new Vector3(transform.position.x, groundPoint.y + minHeight, transform.position.z);
			if (transform.position.y - groundPoint.y <= minHeight)
			{
				transform.position = newPosition;
			}	
		}

		if (Input.GetMouseButtonDown(0))
		{
			bool gotPoint = GetWorldPoint(Camera.main, Input.mousePosition, maxClickDistance, layerMask, out Vector3 clickPoint);
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
	}

	private void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }
	
	private void Zoom(InputAction.CallbackContext context)
	{
		zoom = context.ReadValue<float>();
		Debug.Log(zoom);
	}

	void MoveCamera(Vector3 direction)
	{
		var forwardOnGround = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
		var speed = Input.GetKey(KeyCode.LeftShift) ? cameraSpeed * cameraShiftSpeed : cameraSpeed;
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
