using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Actor3d : MonoBehaviour
{
    
    
    public NavMeshAgent agent;

    public NavMeshAgent Agent
    {
        get {return agent;}
        

    }
    public void Awake()
    {
        NavMeshAgent agent=GetComponent<NavMeshAgent>();

    }
}
