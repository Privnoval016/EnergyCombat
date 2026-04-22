using System;
using UnityEngine;
using StateMachine;

public class PlayerController : MonoBehaviour
{
    #region Variables
    
    #region Components
    
    public StateMachine<PlayerController> StateMachine { get; private set; }
    
    #endregion
    
    #endregion
    
    #region Monobehaviour Callbacks

    private void Awake()
    {
        StateMachine = PlayerStateConstructor.Build(this);
    }
    
    #endregion
}