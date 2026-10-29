using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialCheck0 : MonoBehaviour
{
    [SerializeField] private InputActionProperty menuAction;

    private TutorialTask tutorialTask;

    private void Awake()
    {
        tutorialTask = GetComponent<TutorialTask>();
    }

    private void OnEnable()
    {
        if (menuAction.action != null)
            menuAction.action.Enable();
    }

    private void OnDisable()
    {
        if (menuAction.action != null)
            menuAction.action.Disable();
    }

    void Update()
    {
        if (menuAction.action != null && menuAction.action.WasPressedThisFrame())
        {
            tutorialTask.CompleteTask();
        }
    }
}