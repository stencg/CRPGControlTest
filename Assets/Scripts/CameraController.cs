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
	[Min(10f)] public float cameraWheelSpeed = 500f;

	const string nameMouseWheel = "Mouse ScrollWheel";
	float heightDifference;

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
		heightDifference = transform.position.y - charControllers[0].transform.position.y;
	}

	void Update()
    {
		if (Input.GetMouseButtonDown(0))
		{
			bool gotPoint = GetWorldPoint(Camera.main, Input.mousePosition, maxClickDistance, layerMask, out Vector3 clickPoint);
			if (gotPoint)
			{
				charControllers[0].SetOff(clickPoint);
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

		var forwardOnGround = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
		if (Input.GetKey(KeyCode.W))
			transform.Translate(cameraSpeed * Time.deltaTime * forwardOnGround, Space.World);
		if (Input.GetKey(KeyCode.S))
			transform.Translate(cameraSpeed * Time.deltaTime * -forwardOnGround, Space.World);
		if (Input.GetKey(KeyCode.D))
			transform.Translate(cameraSpeed * Time.deltaTime * Vector3.right, Space.Self);
		if (Input.GetKey(KeyCode.A))
			transform.Translate(cameraSpeed * Time.deltaTime * Vector3.left, Space.Self);

		var mouseWheel = Input.GetAxis(nameMouseWheel) * cameraWheelSpeed;

		bool gotGround = GetWorldGround(Camera.main.transform, 40, layerMask, out Vector3 groundPoint);

		if (!Mathf.Approximately(mouseWheel, 0))
		{
			mouseWheel *= Time.fixedDeltaTime;
			transform.Translate(Vector3.forward * mouseWheel);
			heightDifference -= mouseWheel;
		}

		if (gotGround)
		{
			transform.position = new Vector3(transform.position.x, groundPoint.y + heightDifference, transform.position.z);
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
