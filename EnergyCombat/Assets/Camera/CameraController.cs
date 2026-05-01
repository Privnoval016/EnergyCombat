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

        foreach (var cameraReference in cameraReferences)
        {
            cameraReference.cineCam.Priority = 0;
        }
        
        StateMachine.ChangeState(CreateBaseState());
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #region Camera State Management

    private CameraState CreateBaseState()
    {
        foreach (var cameraReference in cameraReferences)
        {
            if (cameraReference.baseState)
                return cameraReference.Create();
        }
        
        return cameraReferences[0].Create(true);
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

        if (cameraReference.baseState)
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
        public CinemachineCamera cineCam;
        public CamMode cameraMode;
        public bool baseState;
        
        public bool IsMatching(CamMode mode) => mode == cameraMode;
        public CameraState Create(bool overrideBase = false) 
            => new(cineCam, cameraMode, baseState || overrideBase);
    }
    
    #endregion
}

public enum CamMode
{
    ThirdPersonFollow,
    OverRightShoulder
}