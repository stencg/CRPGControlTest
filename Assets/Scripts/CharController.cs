using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class CharController : MonoBehaviour
{
    public NavMeshAgent navMeshAgent;
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
		navMeshAgent.destination = goalPosition;
		called = true;
	}

	public virtual void OnAnimatorMove()
	{
		navMeshAgent.speed = (animator.deltaPosition / Time.deltaTime).magnitude;
	}

	void Update()
    {
		if (navMeshAgent.hasPath
			&& navMeshAgent.path.status == NavMeshPathStatus.PathComplete
			&& Vector3.Distance(transform.position, navMeshAgent.destination) > navMeshAgent.stoppingDistance)
		{
			if (called)
			{
				called = false;
				isMoving = true;
				animator.SetBool(isMovingHash, true);
				navMeshAgent.isStopped = false;
				navMeshAgent.updateRotation = true;
				Debug.Log($"{gameObject.name} has set off", gameObject);
			}
		}
		else if (isMoving)
		{
			isMoving = false;
			animator.SetBool(isMovingHash, false);
			navMeshAgent.isStopped = true;
			navMeshAgent.updateRotation = false;
			Debug.Log($"{gameObject.name} has arrived", gameObject);
		}
	}
}
