using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SelectTest : MonoBehaviour
{
    [HideInInspector] public CubeInfo method;
    [HideInInspector] public BugInstance[] bugs;
    private MethodInfo scenarioMethod;
    private AnalysisReader reader;
    private Renderer targetRenderer;
    private Color originalColor;
    private bool selectionColorApplied;
    private TeacherController teacher;
    private int lastSelectionFrame = -1;

    public void InitializeScenario(AnalysisReader owner, MethodInfo data,
        Canvas canvas, TextMeshProUGUI text, ScrollRect scroll)
    {
        reader = owner;
        scenarioMethod = data;
    }

    public void OnPointerSelect()
    {
        if (method == null || scenarioMethod == null || reader == null) return;
        if (lastSelectionFrame == Time.frameCount) return;
        lastSelectionFrame = Time.frameCount;
        reader.currentlySeclected = method;
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null && !selectionColorApplied)
        {
            originalColor = targetRenderer.material.color;
            targetRenderer.material.color = new Color(.7f, .7f, .7f);
            selectionColorApplied = true;
        }
        reader.InfoPanel?.Show(this, method.FullName, scenarioMethod, false, true);
        ResolveTeacher();
        teacher?.ExplainSelectedCode(method.FullName, scenarioMethod);
    }

    public void ShowSource()
    {
        if (method == null || scenarioMethod == null || reader == null) return;
        reader.InfoPanel?.Show(this, method.FullName, scenarioMethod, false, false);
        ResolveTeacher();
        teacher?.ExplainSelectedCode(method.FullName, scenarioMethod);
    }

    public void ShowSolution()
    {
        if (method == null || scenarioMethod == null || reader == null ||
            string.IsNullOrWhiteSpace(scenarioMethod.fixedCode)) return;
        reader.InfoPanel?.Show(this, method.FullName, scenarioMethod, true, false);
        ResolveTeacher();
        teacher?.ExplainSelectedSolution(method.FullName, scenarioMethod);
    }

    private void ResolveTeacher()
    {
        if (teacher == null) teacher = FindAnyObjectByType<TeacherController>();
    }
    public void OnPointerRelease()
    {
        if (targetRenderer != null && selectionColorApplied)
            targetRenderer.material.color = originalColor;
        selectionColorApplied = false;
    }
    private void OnDisable() { OnPointerRelease(); }
    public void DebugHover() { }
    [ContextMenu("Debug/Test selection")]
    private void DebugSelection() { OnPointerSelect(); }
}
