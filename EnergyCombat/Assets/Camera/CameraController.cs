using System;
using System.Collections.Generic;
using Extensions.PushdownAutomata;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController: MonoBehaviour
{
    #region Components

    private PushdownAutomata<CameraController, CameraState> StateMachine;

    [HideInInspector] public CinemachineBrain brain;

    [SerializeField] private List<CameraReference> cameraReferences;
    
    #endregion

    private void Awake()
    {
        StateMachine = new PushdownAutomata<CameraController, CameraState>(this);
        StateMachine.ChangeState(CreateBaseState());
    }

    #region Camera State Management

    private CameraState CreateBaseState()
    {
        foreach (var cameraReference in cameraReferences)
        {
            if (cameraReference.IsBaseState())
                return cameraReference.Create();
        }

        return null;
    }

    private CameraReference GetCamera(CamMode mode)
    {
        foreach (var cameraReference in cameraReferences)
        {
            if (cameraReference.IsMatching(mode))
                return cameraReference;
        }

        return default;
    }
    
    public void SwitchCamera(CamMode mode)
    {
        var cameraReference = GetCamera(mode);

        if (cameraReference.IsBaseState())
        {
            ReturnToBaseCamera();
            return;
        }
        
        StateMachine.ChangeState(cameraReference.Create());
    }

    public void ResumeLastCamera()
    {
        StateMachine.ResumePrevious();
    }

    public void ReturnToBaseCamera()
    {
        StateMachine.ResumeBase();
    }
    
    #endregion
    
    #region Data
    
    [Serializable]
    public struct CameraReference
    {
        [SerializeField] private CinemachineCamera cineCam;
        [SerializeField] private CamMode cameraMode;
        [SerializeField] private bool baseState;
        
        public bool IsMatching(CamMode mode) => mode == cameraMode;
        public bool IsBaseState() => baseState;
        public CameraState Create() => new CameraState(cineCam, cameraMode, baseState);
    }
    
    #endregion
}

public enum CamMode
{
    ThirdPersonFollow,
    OverRightShoulder
}