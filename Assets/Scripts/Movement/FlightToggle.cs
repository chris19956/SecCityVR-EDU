using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class FlightToggle : MonoBehaviour
{
    [SerializeField] private InputActionReference flyAction;
    [SerializeField] private ContinuousMoveProvider moveProvider;
    private bool enabledHere;
    private void OnEnable()
    {
        if (flyAction == null || flyAction.action == null) return;
        flyAction.action.performed += OnFlyPressed;
        enabledHere = !flyAction.action.enabled;
        if (enabledHere) flyAction.action.Enable();
    }
    private void OnDisable()
    {
        if (flyAction == null || flyAction.action == null) return;
        flyAction.action.performed -= OnFlyPressed;
        if (enabledHere) flyAction.action.Disable();
        enabledHere = false;
    }
    private void OnFlyPressed(InputAction.CallbackContext context)
    {
        if (moveProvider != null) moveProvider.enableFly = !moveProvider.enableFly;
    }
}
