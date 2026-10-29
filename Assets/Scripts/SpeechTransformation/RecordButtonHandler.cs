using UnityEngine;
using UnityEngine.InputSystem;

public class RecordButtonHandler : MonoBehaviour
{
    [SerializeField]
    private InputActionReference leftSecondaryButton;

    private bool wasHeld;
    private bool enabledByThisScript;

    private void OnEnable()
    {
        wasHeld = false;
        enabledByThisScript = false;

        if (leftSecondaryButton == null ||
            leftSecondaryButton.action == null)
        {
            Debug.LogError(
                "Bitte die Input Action für den linken Secondary Button zuweisen.",
                this);
            return;
        }

        if (!leftSecondaryButton.action.enabled)
        {
            leftSecondaryButton.action.Enable();
            enabledByThisScript = true;
        }
    }

    private void Update()
    {
        if (leftSecondaryButton == null ||
            leftSecondaryButton.action == null)
        {
            return;
        }

        InputAction action = leftSecondaryButton.action;
        bool isHeld = action.IsPressed();

        if (isHeld)
        {
            WhileButtonHeld();
        }

        if (action.WasReleasedThisFrame() || (wasHeld && !isHeld))
        {
            OnButtonReleased();
        }

        wasHeld = isHeld;
    }

    private void WhileButtonHeld()
    {

    }

    private void OnButtonReleased()
    {

    }

    private void OnDisable()
    {
        if (enabledByThisScript &&
            leftSecondaryButton != null &&
            leftSecondaryButton.action != null)
        {
            leftSecondaryButton.action.Disable();
        }

        enabledByThisScript = false;
        wasHeld = false;
    }
}