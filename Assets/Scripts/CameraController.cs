using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    public CharController[] charControllers;
	public float maxClickDistance = 100f;
	public LayerMask layerMask;
	public DecalProjector goalMarker;
	[Min(1f)] public float cameraSpeed = 5f;
	[Min(2f)] public float cameraShiftSpeed = 5f;
	[Min(10f)] public float cameraWheelSpeed = 500f;
	[Min(0f)] public float smoothTime = 0.25f;
	[Min(10f)] public float maxDistance = 30f;
	public float minDistance = 10f;

	const string nameMouseWheel = "Mouse ScrollWheel";
	float currentHeight;
	private Vector3 velocity;
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
			Instance = this;
	}

	private void OnEnable()
	{
		selectedPlayer = charControllers[0];
		currentHeight = transform.position.y;
	}

	void Update()
    {
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

		var currentCharPoint = new Vector2(selectedPlayer.transform.position.x, selectedPlayer.transform.position.z);
		var currentCameraPoint = new Vector2(transform.position.x, transform.position.z);
		var forwardOnGround = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

		// Move
		var speed = Input.GetKey(KeyCode.LeftShift) ? cameraSpeed * cameraShiftSpeed : cameraSpeed;
		MoveCamera(KeyCode.W, forwardOnGround, currentCharPoint, currentCameraPoint, speed);
		MoveCamera(KeyCode.S, -forwardOnGround, currentCharPoint, currentCameraPoint, speed);
		MoveCamera(KeyCode.D, transform.right, currentCharPoint, currentCameraPoint, speed);
		MoveCamera(KeyCode.A, -transform.right, currentCharPoint, currentCameraPoint, speed);

		// Zoom
		var mouseWheel = Input.GetAxis(nameMouseWheel) * cameraWheelSpeed;
		mouseWheel *= Time.fixedDeltaTime;
		if (!Mathf.Approximately(mouseWheel, 0) && currentHeight < maxDistance && currentHeight > minDistance)
		{
			var newPosition = transform.position + transform.forward * mouseWheel;
			Vector2 newCameraPoint = new Vector2(newPosition.x, newPosition.z);
			float newDistance = Vector2.Distance(newCameraPoint, currentCharPoint);
			if (newDistance <= maxDistance)
				transform.position = newPosition;
		}

		// Limit by ground
		GetWorldGround(Camera.main.transform, 100, layerMask, out Vector3 groundPoint);
		var desiredHeight = new Vector3(transform.position.x, groundPoint.y + minDistance, transform.position.z);
		if (currentHeight - groundPoint.y < minDistance)
		{
			transform.position = Vector3.SmoothDamp(transform.position, desiredHeight, ref velocity, smoothTime);
		}

		if (!Mathf.Approximately(mouseWheel, 0))
			currentHeight -= mouseWheel;
		currentHeight = Mathf.Clamp(currentHeight, minDistance, maxDistance);

	}

	void MoveCamera(KeyCode key, Vector3 direction, Vector2 currentCharPoint, Vector2 currentCameraPoint, float speed)
	{
		if (Input.GetKey(key))
		{
			Vector3 newPosition = transform.position + speed * Time.deltaTime * direction;
			Vector2 newCameraPoint = new Vector2(newPosition.x, newPosition.z);

			float newDistance = Vector2.Distance(newCameraPoint, currentCharPoint);
			if (newDistance <= maxDistance || newDistance < Vector2.Distance(currentCameraPoint, currentCharPoint))
			{
				transform.position = newPosition;
			}
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
		var drawRay = new Ray(origin.position, Vector3.down);
		// How high above the ground it might be
		var originOffest = Vector3.up * 100;
		var ray = new Ray(origin.position + originOffest, Vector3.down);
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
