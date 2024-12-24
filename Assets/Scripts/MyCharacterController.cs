using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class MyCharacterController : MonoBehaviour
{
    [SerializeField] NavMeshAgent navMeshAgent;
	[SerializeField] Transform goal;
    [SerializeField] Animator animator;
	[SerializeField] [Min(1f)]public float runDistance = 4f;

	private int isMovingHash, runBlendHash;
	private bool isMoving, called;
	private float velocity;

	private void Awake()
	{
		isMovingHash = Animator.StringToHash("IsMoving");
		runBlendHash = Animator.StringToHash("Blend");
		animator.applyRootMotion = false;
	}

	public void SetOff(Vector3 goalPosition)
    {
		navMeshAgent.destination = goalPosition;
		called = true;
	}

	public virtual void OnAnimatorMove()
	{
		if (isMoving)
		{
;			navMeshAgent.speed = (animator.deltaPosition / Time.deltaTime).magnitude;
			var currentBlend = animator.GetFloat(runBlendHash);
			bool needRun = navMeshAgent.remainingDistance > runDistance;
			animator.SetFloat(runBlendHash, needRun
				? Mathf.SmoothDamp(currentBlend, 1, ref velocity, 0.7f) : Mathf.SmoothDamp(currentBlend, 0, ref velocity, 0.7f));
		}
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
			MyCameraController.Instance.HideMarker();
			Debug.Log($"{gameObject.name} has arrived", gameObject);
		}
	}
}
