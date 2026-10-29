using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class VRTutorialPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text buttonText;

    [Header("Bild zur aktuellen Steuerung")]
    [SerializeField] private Image controlImage;

    [SerializeField] private Sprite welcomeSprite;
    [SerializeField] private Sprite moveSprite;

    [SerializeField] private Sprite snapTurnSprite;
    [SerializeField] private Sprite talkSprite;
    [SerializeField] private Sprite selectSprite;
    [SerializeField] private Sprite flySprite;
    [SerializeField] private Sprite menuSprite;

    [Tooltip("Optionales Bild für den Abschluss.")]
    [SerializeField] private Sprite finishSprite;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference snapTurnAction;
    [SerializeField] private InputActionReference talkAction;
    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference flyAction;
    [SerializeField] private InputActionReference menuAction;

    [SerializeField] private TeacherController teacherController;
    [SerializeField] private MainMenuController mainMenuController;

    private bool tutorialCompleted;

    private int currentStep = 0;
    private bool inputRecognized;

    private readonly string[] titles =
    {
        "Willkommen",
        "Bewegen",
        "Drehen",
        "Reden",
        "Interagieren",
        "Fliegen",
        "Menü",
        "Bereit!"
    };

    private readonly string[] descriptions =
    {
        "In diesem Tutorial lernst du die VR-Steuerung kennen.\n\n" +
        "Klicke auf „Weiter“, um zu beginnen.",

        "Bewege den linken Stick in eine beliebige Richtung.",

        "Bewege den rechten Stick nach links oder rechts, " +
        "um dich schrittweise zu drehen.",

        "Drücke Y um mit dem Lehrer zu reden",

        "Drücke die seitliche Taste um ein Gebäude zu inspizieren",

        "Drücke A, um den Flugmodus zu aktivieren.",

        "Drücke X um das Menü zu öffnen.",

        "Du hast alle Eingaben ausprobiert.\n\n" +
        "Viel Spaß beim Erkunden!"
    };

    private void OnEnable()
    {
        currentStep = 0;
        inputRecognized = false;
        tutorialCompleted = false;

        if (nextButton == null ||
            titleText == null ||
            descriptionText == null ||
            buttonText == null)
        {
            Debug.LogError(
                "VRTutorialPanel: UI-Referenzen fehlen.",
                this
            );
            return;
        }

        nextButton.onClick.RemoveListener(NextStep);
        nextButton.onClick.AddListener(NextStep);

        if (moveAction != null && moveAction.action != null)
            moveAction.action.performed += OnMovementRecognized;

        if (snapTurnAction != null && snapTurnAction.action != null)
            snapTurnAction.action.performed += OnSnapTurnRecognized;

        if (talkAction != null && talkAction.action != null)
            talkAction.action.performed += OnTalkRecognized;

        if (selectAction != null && selectAction.action != null)
            selectAction.action.performed += OnSelectRecognized;

        if (flyAction != null && flyAction.action != null)
            flyAction.action.performed += OnFlyRecognized;

        if (menuAction != null && menuAction.action != null)
            menuAction.action.performed += OnMenuRecognized;

        ShowStep();
    }

    private void OnDisable()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextStep);

        if (moveAction != null && moveAction.action != null)
            moveAction.action.performed -= OnMovementRecognized;

        if (snapTurnAction != null && snapTurnAction.action != null)
            snapTurnAction.action.performed -= OnSnapTurnRecognized;

        if (talkAction != null && talkAction.action != null)
            talkAction.action.performed -= OnTalkRecognized;

        if (selectAction != null && selectAction.action != null)
            selectAction.action.performed -= OnSelectRecognized;

        if (flyAction != null && flyAction.action != null)
            flyAction.action.performed -= OnFlyRecognized;

        if (menuAction != null && menuAction.action != null)
            menuAction.action.performed -= OnMenuRecognized;
    }

    private void ShowStep()
    {
        inputRecognized =
            currentStep == 0 ||
            currentStep == titles.Length - 1;

        titleText.text = titles[currentStep];
        descriptionText.text = descriptions[currentStep];

        nextButton.interactable = inputRecognized;
        nextButton.gameObject.SetActive(inputRecognized);

        buttonText.text = currentStep == titles.Length - 1
            ? "Tutorial schließen"
            : "Weiter";

        UpdateControlImage();
    }

    private void UpdateControlImage()
    {
        if (controlImage == null)
            return;

        Sprite sprite = null;

        switch (currentStep)
        {
            case 0:
                sprite = welcomeSprite;
                break;

            case 1:
                sprite = moveSprite;
                break;

            case 2:
                sprite = snapTurnSprite;
                break;

            case 3:
                sprite = talkSprite;
                break;

            case 4:
                sprite = selectSprite;
                break;

            case 5:
                sprite = flySprite;
                break;

            case 6:
                sprite = menuSprite;
                break;

            case 7:
                sprite = finishSprite;
                break;
        }

        controlImage.sprite = sprite;
        controlImage.preserveAspect = true;
        controlImage.raycastTarget = false;
        controlImage.gameObject.SetActive(sprite != null);
    }

    private void OnMovementRecognized(
        InputAction.CallbackContext context)
    {
        if (currentStep == 1)
            UnlockNextButton();
    }

    private void OnSnapTurnRecognized(
        InputAction.CallbackContext context)
    {
        if (currentStep == 2)
            UnlockNextButton();
    }

    private void OnTalkRecognized(
    InputAction.CallbackContext context)
    {
        if (currentStep == 3)
            UnlockNextButton();
    }

    private void OnSelectRecognized(
        InputAction.CallbackContext context)
        {
            if (currentStep == 4)
                UnlockNextButton();
        }

    private void OnFlyRecognized(
        InputAction.CallbackContext context)
    {
        if (currentStep == 5)
            UnlockNextButton();
    }

    private void OnMenuRecognized(
    InputAction.CallbackContext context)
    {
        if (currentStep == 6)
            UnlockNextButton();
    }

    private void UnlockNextButton()
    {
        if (inputRecognized)
            return;

        inputRecognized = true;

        descriptionText.text +=
            "\n\nEingabe erkannt. Du kannst fortfahren.";

        nextButton.gameObject.SetActive(true);
        nextButton.interactable = true;
    }

    public void NextStep()
    {
        if (tutorialCompleted || !inputRecognized)
            return;

        if (currentStep >= titles.Length - 1)
        {
            tutorialCompleted = true;

            gameObject.SetActive(false);

            OnTutorialFinished();
            return;
        }

        currentStep++;
        ShowStep();
    }

    private void OnTutorialFinished()
    {
        if (mainMenuController == null)
        {
            Debug.LogError(
                "MainMenuController im Tutorial nicht zugewiesen.",
                this
            );
            return;
        }

        mainMenuController.OnTutorialFinished();
    }
}