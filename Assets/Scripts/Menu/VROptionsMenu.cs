using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class VROptionsMenu : MonoBehaviour
{
    [Header("Menü")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private InputActionReference menuAction;

    [Header("Position vor dem Spieler")]
    [SerializeField] private float menuDistance = 1.5f;
    [SerializeField] private float menuVerticalOffset = -0.15f;

    [Header("Spieler und Startpunkt")]
    [SerializeField] private XROrigin xrOrigin;

    [Tooltip("Startpunkt auf Bodenhöhe, außerhalb des XR Origin.")]
    [SerializeField] private Transform startPoint;

    [SerializeField] private CharacterController characterController;

    [Header("Vorhandene Controller")]
    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField] private TeacherController teacherController;
    [SerializeField] private AnalysisReader analysisReader;

    [SerializeField] private Behaviour[] movementComponents;

    private readonly Dictionary<Behaviour, bool> movementStates =
        new Dictionary<Behaviour, bool>();

    private bool movementPaused;

    public bool IsOpen { get; private set; }

    private bool enabledActionHere;

    private void Awake()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);

        IsOpen = false;
    }

    private void OnEnable()
    {
        if (menuAction == null || menuAction.action == null)
            return;

        menuAction.action.performed += OnMenuPressed;

        enabledActionHere = !menuAction.action.enabled;

        if (enabledActionHere)
            menuAction.action.Enable();
    }

    private void OnDisable()
    {
        if (menuAction != null && menuAction.action != null)
        {
            menuAction.action.performed -= OnMenuPressed;

            if (enabledActionHere)
                menuAction.action.Disable();
        }

        enabledActionHere = false;
        CloseMenu();
    }

    private void OnMenuPressed(InputAction.CallbackContext context)
    {
        if (IsOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (IsOpen)
            return;

        if (menuRoot == null ||
            xrOrigin == null ||
            xrOrigin.Camera == null)
        {
            Debug.LogWarning("Optionsmenü: Referenzen fehlen.", this);
            return;
        }

        PauseMovement();

        Transform cameraTransform = xrOrigin.Camera.transform;

        Vector3 forward = Vector3.ProjectOnPlane(
            cameraTransform.forward,
            Vector3.up
        );

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.ProjectOnPlane(
                xrOrigin.transform.forward,
                Vector3.up
            );
        }

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        menuRoot.transform.SetPositionAndRotation(
            cameraTransform.position
                + forward * menuDistance
                + Vector3.up * menuVerticalOffset,
            Quaternion.LookRotation(forward, Vector3.up)
        );

        IsOpen = true;
        menuRoot.SetActive(true);
    }

    public void CloseMenu()
    {
        IsOpen = false;

        if (menuRoot != null)
            menuRoot.SetActive(false);

        ResumeMovement();
    }

    public void ReturnToStart()
    {
        if (xrOrigin == null ||
            xrOrigin.Camera == null ||
            startPoint == null)
        {
            Debug.LogWarning("XR Origin oder Startpunkt fehlt.", this);
            return;
        }

        CloseMenu();

        if (analysisReader != null)
            analysisReader.guidedReview = false;

        bool controllerWasEnabled =
            characterController != null &&
            characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        try
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                startPoint.forward,
                Vector3.up
            );

            if (forward.sqrMagnitude > 0.001f)
            {
                xrOrigin.MatchOriginUpCameraForward(
                    Vector3.up,
                    forward.normalized
                );
            }

            float eyeHeight = xrOrigin.CameraInOriginSpaceHeight;

            xrOrigin.MoveCameraToWorldLocation(
                startPoint.position + Vector3.up * eyeHeight
            );
        }
        finally
        {
            if (controllerWasEnabled && characterController != null)
                characterController.enabled = true;
        }
    }

    private void PauseMovement()
    {
        if (movementPaused)
            return;

        movementPaused = true;
        movementStates.Clear();

        if (movementComponents == null)
            return;

        foreach (Behaviour component in movementComponents)
        {
            if (component == null ||
                component == this ||
                movementStates.ContainsKey(component))
            {
                continue;
            }

            movementStates.Add(component, component.enabled);
            component.enabled = false;
        }
    }

    private void ResumeMovement()
    {
        if (!movementPaused)
            return;

        foreach (KeyValuePair<Behaviour, bool> entry in movementStates)
        {
            if (entry.Key != null)
                entry.Key.enabled = entry.Value;
        }

        movementStates.Clear();
        movementPaused = false;
    }

    public void GoToMainMenu()
    {
        CloseMenu();

        if (teacherController != null)
            teacherController.CancelCurrentTalk();

        ReturnToStart();

        if (mainMenuController != null)
            mainMenuController.OnBackButtonPressed();
    }

    public void QuitGame()
    {
        if (teacherController != null)
            teacherController.CancelCurrentTalk();

        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}