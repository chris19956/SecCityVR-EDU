using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TutorialCanvasController : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(2, 4)]
        public string headline;
        [TextArea(3, 6)]
        public string description;
        public Sprite icon; // optional: Controller-Icon für diesen Schritt
    }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI headlineText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI progressText; // z.B. "3 / 6"
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button skipButton;

    [Header("Tutorial Content")]
    [SerializeField]
    private List<TutorialStep> steps = new List<TutorialStep>
    {
        new TutorialStep
        {
            headline = "Willkommen in der VR Code City",
            description = "Diese Umgebung zeigt dir Schwachstellen im Code als Gebäude. Rote und gelbe Markierungen zeigen, wie schwerwiegend ein Problem ist."
        },
        new TutorialStep
        {
            headline = "Nach oben fliegen",
            description = "Halte den linken Grip-Button gedrückt, um nach oben zu fliegen."
        },
        new TutorialStep
        {
            headline = "Nach unten fliegen",
            description = "Halte den rechten Grip-Button gedrückt, um nach unten zu fliegen."
        },
        new TutorialStep
        {
            headline = "Vorwärts bewegen",
            description = "Ziehe den rechten Trigger, um dich in Blickrichtung vorwärts zu bewegen."
        },
        new TutorialStep
        {
            headline = "Rückwärts bewegen",
            description = "Drücke den Thumbstick nach unten, um dich langsamer rückwärts zu bewegen."
        },
        new TutorialStep
        {
            headline = "Zu deinem Lernpartner teleportieren",
            description = "Drücke den sekundären Button, um dich neben deinen Mitspieler zu teleportieren."
        },
        new TutorialStep
        {
            headline = "Geführte Ansicht umschalten",
            description = "Drücke den primären Button, um die geführte Ansicht ein- oder auszuschalten. Dabei folgst du automatisch der Position deines Mitspielers."
        },
        new TutorialStep
        {
            headline = "Los geht's!",
            description = "Du kannst jetzt frei durch die Stadt fliegen und Schwachstellen an den farbigen Markierungen erkennen. Viel Erfolg!"
        }
    };

    [Header("Behaviour")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Optional: Skip-Input direkt in VR")]
    [SerializeField] private InputActionProperty skipTutorialAction;
    [SerializeField] private InputActionProperty nextStepAction;

    private int currentStepIndex = 0;

    private void OnEnable()
    {
        if (nextButton != null) nextButton.onClick.AddListener(NextStep);
        if (previousButton != null) previousButton.onClick.AddListener(PreviousStep);
        if (skipButton != null) skipButton.onClick.AddListener(CloseTutorial);

        if (skipTutorialAction.action != null) skipTutorialAction.action.Enable();
        if (nextStepAction.action != null) nextStepAction.action.Enable();
    }

    private void OnDisable()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(NextStep);
        if (previousButton != null) previousButton.onClick.RemoveListener(PreviousStep);
        if (skipButton != null) skipButton.onClick.RemoveListener(CloseTutorial);

        if (skipTutorialAction.action != null) skipTutorialAction.action.Disable();
        if (nextStepAction.action != null) nextStepAction.action.Disable();
    }

    private void Start()
    {
        if (showOnStart)
            ShowTutorial();
        else
            HideImmediate();
    }

    private void Update()
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0f)
            return;

        if (nextStepAction.action != null && nextStepAction.action.WasPressedThisFrame())
            NextStep();

        if (skipTutorialAction.action != null && skipTutorialAction.action.WasPressedThisFrame())
            CloseTutorial();
    }

    public void ShowTutorial()
    {
        currentStepIndex = 0;
        gameObject.SetActive(true);
        RenderCurrentStep();
        StopAllCoroutines();
        StartCoroutine(FadeTo(1f));
    }

    public void NextStep()
    {
        if (currentStepIndex >= steps.Count - 1)
        {
            CloseTutorial();
            return;
        }

        currentStepIndex++;
        RenderCurrentStep();
    }

    public void PreviousStep()
    {
        if (currentStepIndex <= 0)
            return;

        currentStepIndex--;
        RenderCurrentStep();
    }

    public void CloseTutorial()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutAndDisable());
    }

    private void RenderCurrentStep()
    {
        if (steps.Count == 0)
            return;

        TutorialStep step = steps[currentStepIndex];

        if (headlineText != null)
            headlineText.text = step.headline;

        if (descriptionText != null)
            descriptionText.text = step.description;

        if (iconImage != null)
        {
            iconImage.sprite = step.icon;
            iconImage.enabled = step.icon != null;
        }

        if (progressText != null)
            progressText.text = $"{currentStepIndex + 1} / {steps.Count}";

        if (previousButton != null)
            previousButton.interactable = currentStepIndex > 0;

        if (nextButton != null)
        {
            TextMeshProUGUI nextLabel = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (nextLabel != null)
                nextLabel.text = currentStepIndex >= steps.Count - 1 ? "Fertig" : "Weiter";
        }
    }

    private System.Collections.IEnumerator FadeTo(float targetAlpha)
    {
        if (canvasGroup == null)
            yield break;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float startAlpha = canvasGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private System.Collections.IEnumerator FadeOutAndDisable()
    {
        yield return FadeTo(0f);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        gameObject.SetActive(false);
    }
}