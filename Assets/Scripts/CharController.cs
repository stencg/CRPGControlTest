using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class CharController : MonoBehaviour
{
    public NavMeshAgent meshAgent;
	public Transform goal;
    public Animator animator;

	private int isMovingHash;
	private bool isMoving, called;

	private void Awake()
	{
		isMovingHash = Animator.StringToHash("IsMoving");
		animator.applyRootMotion = false;
	}

	public void SetOff(Vector3 goalPosition)
    {
		meshAgent.destination = goalPosition;
		called = true;
	}

	public virtual void OnAnimatorMove()
	{
		meshAgent.speed = (animator.deltaPosition / Time.deltaTime).magnitude;
	}

	void Update()
    {
		if (meshAgent.hasPath)
		{
			if (called)
			{
				called = false;
				isMoving = true;
				animator.SetBool(isMovingHash, true);
				meshAgent.isStopped = false;
				meshAgent.updateRotation = true;
				Debug.Log($"{gameObject.name} has set off", gameObject);
			}
		}
		else if (isMoving)
		{
			isMoving = false;
			animator.SetBool(isMovingHash, false);
			meshAgent.isStopped = true;
			meshAgent.updateRotation = false;
			Debug.Log($"{gameObject.name} has arrived", gameObject);
		}
	}
}
