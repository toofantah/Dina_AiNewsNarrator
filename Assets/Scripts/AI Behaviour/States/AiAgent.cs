using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AiAgent : MonoBehaviour
{
    public AiStateMachine stateMachine;
    public AiStateId intialState;
    public NavMeshAgent navMeshAgent;
    public AiAgentConfig config;
    public Transform playerTransform;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        navMeshAgent = GetComponent<NavMeshAgent>();
        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterStates(new AiIdleState());
        stateMachine.RegisterStates(new AiChasePlayerState());
        stateMachine.ChangeState(intialState);
    }

    void Update()
    {
        stateMachine.Update();
    }
}
