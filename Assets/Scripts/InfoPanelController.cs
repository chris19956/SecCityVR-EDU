using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InfoPanelController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Camera view;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private Button codeButton;
    [SerializeField] private Button solutionButton;
    [SerializeField] private Button scrollUpButton;
    [SerializeField] private Button scrollDownButton;
    [SerializeField] private Button closeButton;

    private SelectTest selection;
    private bool configured;
    public bool IsShowingSolution { get; private set; }

    public void Configure(Canvas targetCanvas, TMP_Text codeText, Camera camera)
    {
        view = camera != null ? camera : Camera.main;
        if (configured)
        {
            if (canvas != null) canvas.worldCamera = view;
            return;
        }

        canvas = targetCanvas;
        text = codeText;
        if (canvas == null || text == null) return;

        // All UI objects and their layout already exist in the scene.
        var root = canvas.transform;
        var viewport = root.Find("CodeViewport");
        scroll = viewport != null ? viewport.GetComponent<ScrollRect>() : null;
        codeButton = FindButton(root, "CodeButton");
        solutionButton = FindButton(root, "SolutionButton");
        scrollUpButton = FindButton(root, "ScrollUpButton");
        scrollDownButton = FindButton(root, "ScrollDownButton");
        closeButton = FindButton(root, "CloseButton");

        if (scroll == null || scroll.viewport == null ||
            scroll.content != text.rectTransform || codeButton == null ||
            solutionButton == null || scrollUpButton == null ||
            scrollDownButton == null || closeButton == null)
        {
            Debug.LogError("InfoPanelController: Die Scene-Hierarchie oder die UI-Referenzen " +
                "sind unvollstaendig. Bitte die mitgelieferte CodeCityScene verwenden.", this);
            return;
        }

        canvas.worldCamera = view;
        codeButton.onClick.AddListener(ShowSource);
        solutionButton.onClick.AddListener(ShowSolution);
        scrollUpButton.onClick.AddListener(ScrollUp);
        scrollDownButton.onClick.AddListener(ScrollDown);
        closeButton.onClick.AddListener(Hide);
        configured = true;
        Hide();
    }

    private static Button FindButton(Transform root, string name)
    {
        var child = root.Find(name);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private void ShowSource() => selection?.ShowSource();
    private void ShowSolution() => selection?.ShowSolution();
    private void ScrollUp() => ScrollPage(1);
    private void ScrollDown() => ScrollPage(-1);

    public static Pose ReadingPose(Transform camera, float distance = 1.5f)
    {
        var forward = Vector3.ProjectOnPlane(camera.forward, Vector3.up);
        if (forward.sqrMagnitude < .001f)
            forward = Vector3.Cross(camera.right, Vector3.up);
        if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
        forward.Normalize();
        return new Pose(camera.position + forward * distance + Vector3.down * .12f,
            Quaternion.LookRotation(forward, Vector3.up));
    }

    public void Show(SelectTest selected, string fullName, MethodInfo method,
        bool solution, bool reposition)
    {
        if (!configured || method == null) return;
        selection = selected;
        IsShowingSolution = solution;
        canvas.gameObject.SetActive(true);
        canvas.enabled = true;
        if (view == null) view = Camera.main;
        if (view != null)
        {
            canvas.worldCamera = view;
            if (reposition)
            {
                var pose = ReadingPose(view.transform);
                canvas.transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        var code = solution ? method.fixedCode : method.vulnerableCode;
        text.text = fullName + (solution ? "\n\nMoegliche Korrektur:\n\n" : "\n\nQuellcode:\n\n") +
            (string.IsNullOrWhiteSpace(code) ? "Kein Code hinterlegt." : code);
        if (solution) text.text += "\n\n" + method.explanation;
        solutionButton.interactable = !string.IsNullOrWhiteSpace(method.fixedCode);

        Canvas.ForceUpdateCanvases();
        text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(scroll.viewport.rect.height,
                text.GetPreferredValues(text.text, text.rectTransform.rect.width, 0).y + 28));
        Canvas.ForceUpdateCanvases();
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 1;
    }

    private void ScrollPage(int direction)
    {
        if (scroll == null || scroll.content == null || scroll.viewport == null) return;
        float excess = scroll.content.rect.height - scroll.viewport.rect.height;
        if (excess <= 0) return;
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition +
            direction * scroll.viewport.rect.height * .8f / excess);
    }

    public void Hide()
    {
        selection = null;
        IsShowingSolution = false;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (!configured) return;
        if (codeButton != null) codeButton.onClick.RemoveListener(ShowSource);
        if (solutionButton != null) solutionButton.onClick.RemoveListener(ShowSolution);
        if (scrollUpButton != null) scrollUpButton.onClick.RemoveListener(ScrollUp);
        if (scrollDownButton != null) scrollDownButton.onClick.RemoveListener(ScrollDown);
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }
}