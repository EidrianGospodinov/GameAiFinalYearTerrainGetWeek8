using UnityEngine;
using UnityEngine.AI;

public class agentController : MonoBehaviour
{
    NavMeshAgent agent;
    [SerializeField] private Transform targetPosition;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ManualMoveToPoint();
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (targetPosition != null)
            {
                MoveToTarget(targetPosition.position);
            }
        }
    }

    private void ManualMoveToPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            agent.SetDestination(hit.point);
        }
    }

    public void MoveToTarget(Vector3 targetPosition)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.SetDestination(targetPosition);
            Debug.Log($"Agent moving to artefact at: {targetPosition}");
        }
    }
}
