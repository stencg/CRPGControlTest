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

	const string msWheel = "Mouse ScrollWheel";

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

	void Update()
    {
		// Smooth movement
		if (Input.GetMouseButtonDown(0))
		{
			bool gotSome = GetWorldPoint(Camera.main, Input.mousePosition, maxClickDistance, layerMask, out Vector3 position);
			if (gotSome)
			{
				charControllers[0].SetOff(position);
				goalMarker.transform.position = position;
				ShowMarker();
				Debug.Log($"Goal: {position}");
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
	}
	private void FixedUpdate()
	{	
		// Framerate independed movement
		var mouseWheel = Input.GetAxis(msWheel) * cameraWheelSpeed;
		mouseWheel *= Time.fixedDeltaTime;
		transform.Translate(Vector3.forward * mouseWheel);
	}
	public static bool GetWorldPoint(Camera camera, Vector2 screenPosition, float distance, LayerMask mask, out Vector3 worldPosition)
	{
		var ray = camera.ScreenPointToRay(screenPosition);
		worldPosition = Vector3.zero;
		var raycast = Physics.Raycast(ray, out var hit, distance, mask, QueryTriggerInteraction.UseGlobal);
		if (raycast)
		{
			worldPosition = hit.point;
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
