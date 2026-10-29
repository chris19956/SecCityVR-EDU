using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject freeModePanel;
    [SerializeField] private GameObject guidedLearningPanel;
    [SerializeField] private GameObject tutorialPanel;

    [Header("Lehrer und Tafel")]
    [SerializeField] private GameObject teacher;
    [SerializeField] private GameObject chalkBoard;
    [SerializeField] private TeacherController teacherController;

    [Header("Buttons")]
    [SerializeField] private Button guidedLesson1;
    [SerializeField] private Button guidedLesson2;
    [SerializeField] private Button recordButton;

    [Header("Canvas")]
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private Transform targetPosition;

    [Header("Audio")]
    [SerializeField] private AudioSource teacherAudioSource;

    [Header("CodeCity")]
    [SerializeField] private AnalysisReader analysisReader;

    private Vector3 originalCanvasPosition;
    private Quaternion originalCanvasRotation;

    private bool canvasPoseStored;
    private bool waitingForTutorial;
    private string selectedLessonResource = "ShopCity_BrokenAccessControl";

    private void Awake()
    {
        StoreCanvasPose();
    }

    private void Start()
    {
        ShowMain();
    }

    private void StoreCanvasPose()
    {
        if (canvasPoseStored || canvasTransform == null)
            return;

        originalCanvasPosition = canvasTransform.localPosition;
        originalCanvasRotation = canvasTransform.localRotation;

        canvasPoseStored = true;
    }

    public void ShowMain()
    {
        waitingForTutorial = false;

        StopCurrentSession();
        ResetCanvasPosition();
        ShowOnlyPanel(startPanel);
    }

    public void ShowModeSelect()
    {
        waitingForTutorial = false;

        StopCurrentSession();
        ResetCanvasPosition();
        ShowOnlyPanel(modePanel);
    }

    public void ShowGuidedLearningSelect()
    {
        waitingForTutorial = false;

        StopCurrentSession();
        ResetCanvasPosition();
        ShowOnlyPanel(guidedLearningPanel);

        if (guidedLesson1 != null)
            guidedLesson1.gameObject.SetActive(true);

        if (guidedLesson2 != null)
            guidedLesson2.gameObject.SetActive(true);
    }

    public void StartGuidedLearning()
    {
        selectedLessonResource = "Scenarios/ShopCity_BrokenAccessControl";
        StartSelectedGuidedLesson(selectedLessonResource);
    }

    public void StartSqlInjectionLearning()
    {
        selectedLessonResource = "Scenarios/ShopCity_SQLInjection";
        StartSelectedGuidedLesson(selectedLessonResource);
    }

    private void StartSelectedGuidedLesson(string selectedLessonResource)
    {
        Debug.Log("[MainMenu] Guided Learning angefordert.", this);
        analysisReader.generateCityWithPackageAnalysis(selectedLessonResource);

        if (tutorialPanel == null)
        {
            Debug.LogError(
                "[MainMenu] Tutorial Panel fehlt.",
                this
            );
            return;
        }

        waitingForTutorial = false;

        StopCurrentSession();
        ResetCanvasPosition();

        waitingForTutorial = true;

        ShowOnlyPanel(tutorialPanel);

        Debug.Log(
            $"[MainMenu] Tutorial geöffnet: " +
            $"activeSelf={tutorialPanel.activeSelf}, " +
            $"activeInHierarchy={tutorialPanel.activeInHierarchy}",
            tutorialPanel
        );
    }

    public void OnTutorialFinished()
    {
        Debug.Log("[MainMenu] Tutorialabschluss empfangen.", this);

        if (!waitingForTutorial)
        {
            Debug.LogWarning(
                "[MainMenu] Abschluss ignoriert: " +
                "Es wird aktuell nicht auf das Tutorial gewartet.",
                this
            );
            return;
        }

        waitingForTutorial = false;

        BeginGuidedLesson();
    }

    public void BeginGuidedLesson()
    {
        Debug.Log("[MainMenu] Fachliche Lerneinheit startet.", this);

        if (teacherController == null)
        {
            Debug.LogError(
                "[MainMenu] TeacherController fehlt.",
                this
            );
            return;
        }

        var lesson = Resources.Load<TextAsset>(selectedLessonResource);
        if (analysisReader == null || lesson == null || !analysisReader.GenerateCityFromJson(lesson.text))
        {
            Debug.LogError("Selected lesson could not be loaded: " + selectedLessonResource, this);
            ShowGuidedLearningSelect();
            return;
        }
        teacherController.ConfigureScenario(analysisReader.CurrentScenario);
        HideAllPanels();

        MoveCanvas();
        if (teacher != null)
            teacher.SetActive(true);

        teacherController.gameObject.SetActive(true);
        teacherController.enabled = true;

        if (chalkBoard != null)
            chalkBoard.SetActive(true);

        if (teacherController.chalkBoardBoard != null)
            teacherController.chalkBoardBoard.SetActive(true);

        if (!teacherController.isActiveAndEnabled)
        {
            Debug.LogError(
                "[MainMenu] Der TeacherController ist weiterhin inaktiv. " +
                "Wahrscheinlich ist ein übergeordnetes Objekt deaktiviert.",
                teacherController
            );
            return;
        }

        if (teacher != null && !teacher.activeInHierarchy)
        {
            Debug.LogError(
                "[MainMenu] Das Avatar-Objekt hat einen inaktiven Parent.",
                teacher
            );
        }

        if (chalkBoard != null && !chalkBoard.activeInHierarchy)
        {
            Debug.LogError(
                "[MainMenu] Die Tafel hat einen inaktiven Parent.",
                chalkBoard
            );
        }
        teacherController.StartGuidedLearning();
    }

    public void StartFreeMode()
    {
        waitingForTutorial = false;

        StopCurrentSession();
        ShowOnlyPanel(freeModePanel);
        MoveCanvas();

        if (teacher != null)
            teacher.SetActive(true);

        if (chalkBoard != null)
            chalkBoard.SetActive(true);

        if (recordButton != null)
        {
            recordButton.gameObject.SetActive(true);
            recordButton.interactable = false;
        }

        if (teacherController != null)
        {
            teacherController.gameObject.SetActive(true);
            teacherController.enabled = true;
            teacherController.StartFreeSession(analysisReader != null ? analysisReader.CurrentScenario : null);
            teacherController.Talk(
                "Du bist aktuell im Modus: free mode. " +
                "Der Nutzer hat den freien Lernmodus geöffnet. " +
                "Erkläre kurz, dass er über den Aufnahmebutton " +
                "Fragen zur IT-Sicherheit und zur CodeCity stellen kann. " +
                "Stelle keine Wissensfrage. " +
                "Verwende das vorgegebene Format Antwort: und Tafel:."
            );
        }
        else if (recordButton != null)
        {
            Debug.LogError(
                "TeacherController ist nicht zugewiesen.",
                this
            );

            recordButton.interactable = true;
        }
    }

    public void MoveCanvas()
    {
        if (canvasTransform == null || targetPosition == null)
            return;

        StoreCanvasPose();

        canvasTransform.SetPositionAndRotation(
            targetPosition.position,
            targetPosition.rotation
        );
    }

    public void ResetCanvasPosition()
    {
        if (canvasTransform == null || !canvasPoseStored)
            return;

        canvasTransform.SetLocalPositionAndRotation(
            originalCanvasPosition,
            originalCanvasRotation
        );
    }

    public void OnBackButtonPressed()
    {
        ShowMain();
    }

    private void StopCurrentSession()
    {
        if (teacherController != null)
            teacherController.CancelCurrentTalk();

        if (teacherAudioSource != null)
            teacherAudioSource.Stop();

        if (analysisReader != null)
        {
            analysisReader.guidedReview = false;
            analysisReader.InfoPanel?.Hide();
        }

        if (recordButton != null)
        {
            recordButton.interactable = false;
            recordButton.gameObject.SetActive(false);
        }

        if (chalkBoard != null)
            chalkBoard.SetActive(false);

        if (teacher != null)
            teacher.SetActive(false);
    }

    private void HideAllPanels()
    {
        SetPanelActive(startPanel, false);
        SetPanelActive(modePanel, false);
        SetPanelActive(freeModePanel, false);
        SetPanelActive(guidedLearningPanel, false);
        SetPanelActive(tutorialPanel, false);
    }

    private void ShowOnlyPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogError(
                "Das gewünschte Menüpanel ist nicht zugewiesen.",
                this
            );
            return;
        }

        HideAllPanels();
        panel.SetActive(true);

        if (!panel.activeInHierarchy)
        {
            Debug.LogError(
                $"Panel '{panel.name}' ist lokal aktiv, aber ein " +
                "übergeordnetes GameObject ist deaktiviert. " +
                "Prüfe die Menü-Hierarchy.",
                panel
            );
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}