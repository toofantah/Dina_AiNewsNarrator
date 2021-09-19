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

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        stateMachine = new AiStateMachine(this);
        stateMachine.RegisterStates(new AiChasePlayerState());
        stateMachine.ChangeState(intialState);
    }

    void Update()
    {
        stateMachine.Update();
    }
}
