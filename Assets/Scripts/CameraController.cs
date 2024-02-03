using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    public CharController[] charControllers;
	public float maxDistance = 100f;
	public LayerMask layerMask;
	void Update()
    {
		if (Input.GetMouseButtonDown(0))
		{
			bool gotSome = GetWorldPoint(Camera.main, Input.mousePosition, maxDistance, layerMask, out Vector3 position);
			if (gotSome)
			{
				charControllers[0].SetOff(position);
				Debug.Log($"Goal: {position}");
			}
			else
				Debug.Log("Nothing");
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
			return true;
		}
		return false;
	}
}
