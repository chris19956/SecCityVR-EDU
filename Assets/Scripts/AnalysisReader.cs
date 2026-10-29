using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.UI;

public class CityMethod
{
    public string name { get; set; }
    public string className { get; set; }
    public GameObject gameObject { get; set; }
    public List<string> outgoingCalls { get; set; }
    public List<string> incomingCalls { get; set; }
    public float width { get; set; }
    public float length { get; set; }
}

public class AnalysisReader : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TextAsset jsonFile;
    [SerializeField] private TextAsset xmlFile;
    [SerializeField] private TextAsset packageAnalysisJson;

    [Header("Scenario UI (assign scene objects here)")]
    [SerializeField] private Canvas infoCanvas;
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private ScrollRect codeScroll;
    [SerializeField] private GameObject basePlateObject;

    [Header("Call graph")]
    [SerializeField] private bool drawCallsAfterGeneration = true;
    [SerializeField, Min(0.001f)] private float callLineWidth = 0.08f;
    [SerializeField, Min(0.1f)] private float callArcHeight = 4f;
    [SerializeField, Range(8, 128)] private int callLineSegments = 32;

    private readonly Dictionary<string, CityMethod> methodsByName =
        new Dictionary<string, CityMethod>(StringComparer.Ordinal);

    private GameObject generatedCityRoot;

    public InfoPanelController InfoPanel { get; private set; }
    public PackageAnalysisResult CurrentScenario { get; private set; }

    private readonly HashSet<string> renderedEdges = new HashSet<string>();

    [Header("Prefabs")]
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private GameObject linePrefab;

    [Header("Layout")]
    [SerializeField] private float scaleMultiplier = 1.0f;
    [SerializeField] private float heightMultiplier = 0.01f;
    [SerializeField] private float spacingFactor = 0.95f;
    [SerializeField] private float minFootprintSize = 0.5f;
    [SerializeField] private float maxHops = 0f;

    [Header("Filters")]
    [SerializeField] private bool onlyPackages = false;
    [SerializeField] private bool onlyClasses = false;

    [Header("Modes")]
    [SerializeField] public bool guidedReview = false;
    [SerializeField] public bool offlineMode = false;

    [Header("Project")]
    [SerializeField] public string ownPackageName = "vr.codecity.scenario";

    [Header("XR Rig")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Transform rigTransform;
    [SerializeField] private Camera xrCamera;

    List<CityMethod> calls = new List<CityMethod>();

    [HideInInspector] public BugCollection bugCollection;
    [HideInInspector] public CubeInfo currentlySeclected;

    int cityWidth = 0;
    int cityHeight = 0;

    public GameObject parentMesh;
    public List<GameObject> childMeshes = new List<GameObject>();

    void Awake()
    {
        if (xrOrigin == null)
        {
            xrOrigin = FindAnyObjectByType<XROrigin>();
        }

        if (xrCamera == null && xrOrigin != null)
        {
            xrCamera = xrOrigin.Camera;
        }

        if (xrCamera == null)
        {
            xrCamera = Camera.main;
        }

        InfoPanel = GetComponent<InfoPanelController>();

        if (InfoPanel == null)
        {
            InfoPanel = gameObject.AddComponent<InfoPanelController>();
        }

        InfoPanel.Configure(infoCanvas, codeText, xrCamera);
    }

    public void generateCityWithPackageAnalysis(string lessonResource)
    {
        if (string.IsNullOrWhiteSpace(lessonResource))
        {
            Debug.LogError("No lesson resource was selected.", this);
            return;
        }

        TextAsset selectedJson = Resources.Load<TextAsset>(lessonResource);

        if (selectedJson == null)
        {
            Debug.LogError(
                $"Lesson JSON could not be loaded from Resources: {lessonResource}",
                this
            );

            return;
        }

        packageAnalysisJson = selectedJson;
        GenerateCityFromJson(packageAnalysisJson.text);
    }

    public bool GenerateCityFromJson(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Scenario JSON is empty.");
            }

            var result = JsonUtility.FromJson<PackageAnalysisResult>(json);

            if (result == null ||
                result.packages == null ||
                result.packages.Length == 0)
            {
                throw new ArgumentException("Scenario has no packages.");
            }

            if (buildingPrefab == null ||
                buildingPrefab.GetComponent<SelectTest>() == null ||
                buildingPrefab.GetComponent<Renderer>() == null)
            {
                throw new ArgumentException(
                    "Building prefab needs SelectTest and Renderer on its root."
                );
            }

            if (basePlateObject == null)
            {
                basePlateObject = GameObject.Find("BasePlate");
            }

            if (basePlateObject == null ||
                basePlateObject.GetComponent<Renderer>() == null)
            {
                throw new ArgumentException(
                    "Assign Base Plate Object (a cube with Renderer)."
                );
            }

            if (heightMultiplier <= 0 ||
                scaleMultiplier <= 0 ||
                spacingFactor <= 0 ||
                spacingFactor > 1)
            {
                throw new ArgumentException(
                    "Height/scale must be positive; spacing must be in (0,1]."
                );
            }

            if (infoCanvas == null || codeText == null)
            {
                Debug.LogWarning(
                    "Assign Info Canvas and Code Text on this scene AnalysisReader.",
                    this
                );
            }

            if (onlyPackages || onlyClasses)
            {
                Debug.LogWarning(
                    "Only Packages/Only Classes hides the method markers. " +
                    "Disable both for this scenario.",
                    this
                );
            }

            BuildScenarioCity(result);
            CurrentScenario = result;
            BuildMethodLookup();

            if (drawCallsAfterGeneration && !onlyPackages && !onlyClasses)
            {
                RenderAllCalls();
            }

            Debug.Log(
                "Scenario loaded: " +
                result.packages.Length +
                " packages, " +
                result.packages
                    .SelectMany(p => p.classes)
                    .SelectMany(c => c.methods)
                    .Count(m => m.severity == 1) +
                " red markers, " +
                result.packages
                    .SelectMany(p => p.classes)
                    .SelectMany(c => c.methods)
                    .Count(m => m.severity == 2) +
                " yellow markers. Semantic correctness still requires review.",
                this
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[AnalysisReader] Scenario could not be loaded: " + ex.Message,
                this
            );

            return false;
        }
    }

    private void BuildScenarioCity(PackageAnalysisResult result)
    {
        PackageInfo[] packages = result.packages;
        int packagesum = packages.Sum(p => p.linesOfCode);

        cityWidth = Math.Max(
            20,
            (int)Math.Round(packagesum * scaleMultiplier / 100)
        );

        cityHeight = Math.Max(
            20,
            (int)Math.Round(packagesum * scaleMultiplier / 100)
        );

        if (rigTransform != null)
        {
            rigTransform.position = new Vector3(
                cityWidth / 2f,
                10f,
                cityHeight / 2f
            );
        }

        if (generatedCityRoot != null)
        {
            generatedCityRoot.SetActive(false);
            Destroy(generatedCityRoot);
        }

        calls.Clear();
        methodsByName.Clear();
        renderedEdges.Clear();
        InfoPanel?.Hide();
        childMeshes.Clear();

        currentlySeclected = null;
        bugCollection = null;

        if (infoCanvas != null)
        {
            infoCanvas.enabled = false;
        }

        generatedCityRoot = new GameObject(
            "GeneratedCity-" + result.projectName
        );

        GameObject basePlate = basePlateObject;
        basePlate.name = "CityCanvas-" + result.projectName;

        basePlate.GetComponent<Renderer>().material.color =
            new Color(0.5f, 0.5f, 0.5f);

        basePlate.transform.localScale = new Vector3(
            cityWidth,
            0.5f,
            cityHeight
        );

        basePlate.transform.position = new Vector3(
            cityWidth / 2f,
            0.25f,
            cityHeight / 2f
        );

        parentMesh = basePlate;

        var sortedPackages = packages
            .Where(p => p.linesOfCode > 0)
            .OrderByDescending(p => p.linesOfCode)
            .ToArray();

        double[] sortedPackageSizes = sortedPackages
            .Select(p => (double)p.linesOfCode)
            .ToArray();

        LayoutRectangle[] packagesRectangles = TreeMap.GetRectangles(
            sortedPackageSizes,
            cityWidth,
            cityHeight
        );

        int pkgCount = sortedPackages.Length;

        for (int i = 0; i < pkgCount; i++)
        {
            if (i >= packagesRectangles.Length)
            {
                Debug.LogWarning(
                    $"Kein Layout-Rechteck für Paket {i} vorhanden, überspringe."
                );

                continue;
            }

            PackageInfo pkg = sortedPackages[i];
            LayoutRectangle pkgRect = packagesRectangles[i];
            Color pkgColor = new Color(0.75f, 0.75f, 0.75f);

            if (pkg.name == ownPackageName)
            {
                pkgColor = new Color(1, 0f, 1);
            }

            float pkgWidth = Mathf.Min(
                pkgRect.Width,
                Mathf.Max(
                    pkgRect.Width * spacingFactor,
                    minFootprintSize
                )
            );

            float pkgLength = Mathf.Min(
                pkgRect.Length,
                Mathf.Max(
                    pkgRect.Length * spacingFactor,
                    minFootprintSize
                )
            );

            GameObject packageContainer = RenderCube(
                pkg.linesOfCode,
                $"Package-{i}",
                pkg.name,
                pkg.name,
                pkgColor,
                new Vector3(pkgRect.X, 0.75f, pkgRect.Y),
                new Vector3(pkgWidth, 0.5f, pkgLength),
                generatedCityRoot
            );

            if (onlyPackages)
            {
                continue;
            }

            var sortedClasses = pkg.classes
                .Where(c => c.linesOfCode > 0)
                .OrderByDescending(c => c.linesOfCode)
                .ToArray();

            double[] sortedClassSizes = sortedClasses
                .Select(c => (double)c.linesOfCode)
                .ToArray();

            LayoutRectangle[] classRectangles = TreeMap.GetRectangles(
                sortedClassSizes,
                pkgWidth,
                pkgLength
            );

            for (int j = 0; j < classRectangles.Length; j++)
            {
                if (j >= sortedClasses.Length)
                {
                    continue;
                }

                ClassInfo cls = sortedClasses[j];
                LayoutRectangle clsRect = classRectangles[j];

                float clsX = clsRect.X + pkgRect.X;
                float clsY = clsRect.Y + pkgRect.Y;

                Color classColor = new Color(0.75f, 0.75f, 0.75f);

                float clsWidth = Mathf.Min(
                    clsRect.Width,
                    Mathf.Max(
                        clsRect.Width * spacingFactor,
                        minFootprintSize
                    )
                );

                float clsLength = Mathf.Min(
                    clsRect.Length,
                    Mathf.Max(
                        clsRect.Length * spacingFactor,
                        minFootprintSize
                    )
                );

                float clsHeight =
                    cls.linesOfCode * heightMultiplier + 0.005f;

                GameObject classContainer = RenderCube(
                    cls.linesOfCode,
                    $"Class-{j}",
                    cls.name,
                    cls.name,
                    classColor,
                    new Vector3(
                        clsX,
                        cls.linesOfCode / 2f * heightMultiplier + 1,
                        clsY
                    ),
                    new Vector3(clsWidth, clsHeight, clsLength),
                    packageContainer,
                    true,
                    null,
                    false
                );

                if (onlyClasses)
                {
                    continue;
                }

                for (int k = 0; k < cls.methods.Count(); k++)
                {
                    MethodInfo methodFromHierarchy = cls.methods[k];

                    int classStart = cls.startLine;
                    int classEnd = cls.endLine;
                    int classHeight = 1 + classEnd - classStart;

                    float sizeToLocRatio =
                        cls.linesOfCode / (float)classHeight;

                    int methodStart = methodFromHierarchy.startLine;
                    int methodEnd = methodFromHierarchy.endLine;

                    float methodHeight = 1 + methodEnd - methodStart;
                    float mappedMethodHeight =
                        methodHeight * sizeToLocRatio;

                    float methodWidth = clsWidth * 1.05f;
                    float methodLength = clsLength * 1.05f;

                    bool renderCubeActive =
                        methodFromHierarchy.severity > 0;

                    Color methodColor;

                    if (methodFromHierarchy.severity == 1)
                    {
                        methodColor = Color.red;
                    }
                    else if (methodFromHierarchy.severity == 2)
                    {
                        methodColor = Color.yellow;
                    }
                    else
                    {
                        methodColor = Color.blue;
                    }

                    GameObject methodContainer = RenderCube(
                        (int)mappedMethodHeight,
                        $"Method-{k}",
                        methodFromHierarchy.name,
                        $"{cls.name}.{methodFromHierarchy.name}",
                        methodColor,
                        new Vector3(
                            clsX - clsWidth * 0.025f,
                            (
                                (methodStart - classStart) * sizeToLocRatio +
                                mappedMethodHeight / 2f
                            ) * heightMultiplier + 1,
                            clsY - clsLength * 0.025f
                        ),
                        new Vector3(
                            methodWidth,
                            mappedMethodHeight * heightMultiplier,
                            methodLength
                        ),
                        classContainer,
                        renderCubeActive,
                        null,
                        false
                    );

                    SelectTest selector =
                        methodContainer.GetComponentInChildren<SelectTest>(true);

                    if (selector != null)
                    {
                        selector.InitializeScenario(
                            this,
                            methodFromHierarchy,
                            infoCanvas,
                            codeText,
                            codeScroll
                        );
                    }

                    CityMethod cm = new CityMethod()
                    {
                        name = $"{cls.name}.{methodFromHierarchy.name}",
                        className = cls.name,
                        gameObject = methodContainer,
                        width = methodWidth,
                        length = methodLength,
                        incomingCalls = new List<string>(),
                        outgoingCalls = new List<string>()
                    };

                    if (methodFromHierarchy.callsTo?.Length > 0)
                    {
                        cm.outgoingCalls = NormalizeCallNames(
                            methodFromHierarchy.callsTo
                        );
                    }

                    if (methodFromHierarchy.callsFrom?.Length > 0)
                    {
                        cm.incomingCalls = NormalizeCallNames(
                            methodFromHierarchy.callsFrom
                        );
                    }

                    calls.Add(cm);
                }
            }
        }
    }

    private static List<string> NormalizeCallNames(string[] references)
    {
        return (references ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private void BuildMethodLookup()
    {
        methodsByName.Clear();

        foreach (CityMethod method in calls)
        {
            if (methodsByName.ContainsKey(method.name))
            {
                throw new ArgumentException(
                    "Duplicate method name: " + method.name
                );
            }

            methodsByName.Add(method.name, method);
        }

        foreach (CityMethod method in calls)
        {
            foreach (string target in method.outgoingCalls)
            {
                if (!methodsByName.ContainsKey(target))
                {
                    Debug.LogWarning(
                        $"Unknown callsTo: {method.name} -> {target}",
                        this
                    );
                }
            }

            foreach (string source in method.incomingCalls)
            {
                if (!methodsByName.ContainsKey(source))
                {
                    Debug.LogWarning(
                        $"Unknown callsFrom: {source} -> {method.name}",
                        this
                    );
                }
            }
        }
    }

    public void RenderAllCalls()
    {
        if (linePrefab == null)
        {
            Debug.LogWarning(
                "Assign Line Prefab to display call connections.",
                this
            );

            return;
        }

        foreach (CityMethod method in calls)
        {
            RenderAllCallsForMethod(method);
        }

        Debug.Log(
            $"Call graph: {calls.Count} methods, " +
            $"{renderedEdges.Count} rendered edges.",
            this
        );
    }

    private void DrawLineFromMethodToMethod(
        CityMethod from,
        CityMethod to
    )
    {
        if (from?.gameObject == null ||
            to?.gameObject == null ||
            linePrefab == null)
        {
            return;
        }

        string edgeKey = from.name + " -> " + to.name;

        if (renderedEdges.Contains(edgeKey))
        {
            return;
        }

        GameObject spawnedLine = Instantiate(
            linePrefab,
            generatedCityRoot.transform,
            false
        );

        spawnedLine.name = "Call-" + edgeKey;
        spawnedLine.transform.localScale = Vector3.one;
        spawnedLine.SetActive(true);

        LineRenderer lineRenderer =
            spawnedLine.GetComponentInChildren<LineRenderer>(true);

        if (lineRenderer == null)
        {
            Debug.LogWarning(
                "Line Prefab has no LineRenderer, including its children.",
                this
            );

            Destroy(spawnedLine);
            return;
        }

        for (
            Transform t = lineRenderer.transform;
            t != spawnedLine.transform;
            t = t.parent
        )
        {
            t.gameObject.SetActive(true);
        }

        lineRenderer.enabled = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;

        lineRenderer.widthCurve = AnimationCurve.Linear(
            0f,
            1f,
            1f,
            1f
        );

        lineRenderer.widthMultiplier = callLineWidth;
        lineRenderer.startColor = Color.blue;
        lineRenderer.endColor = Color.white;

        // RenderCube places its visible cube at this local center.
        Vector3 start = from.gameObject.transform.TransformPoint(
            new Vector3(0.5f, 0f, 0.5f)
        );

        Vector3 end = to.gameObject.transform.TransformPoint(
            new Vector3(0.5f, 0f, 0.5f)
        );

        Vector3 control = (start + end) * 0.5f;
        control.y = Mathf.Max(start.y, end.y) + 2f * callArcHeight;

        if (from.className == to.className)
        {
            control.x += callArcHeight;
        }

        DrawQuadraticBezierCurve(
            lineRenderer,
            start,
            control,
            end
        );

        renderedEdges.Add(edgeKey);
    }

    private void DrawQuadraticBezierCurve(
        LineRenderer lineRenderer,
        Vector3 start,
        Vector3 control,
        Vector3 end
    )
    {
        int count = Mathf.Max(2, callLineSegments);
        var points = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float u = 1f - t;

            points[i] =
                u * u * start +
                2f * u * t * control +
                t * t * end;
        }

        lineRenderer.positionCount = count;
        lineRenderer.SetPositions(points);
    }

    private void RenderAllCallsForMethod(CityMethod method)
    {
        foreach (string targetName in method.outgoingCalls)
        {
            if (methodsByName.TryGetValue(
                targetName,
                out CityMethod target
            ))
            {
                DrawLineFromMethodToMethod(method, target);
            }
        }

        foreach (string sourceName in method.incomingCalls)
        {
            if (methodsByName.TryGetValue(
                sourceName,
                out CityMethod source
            ))
            {
                DrawLineFromMethodToMethod(source, method);
            }
        }
    }

    public void RenderCallsForMethodName(string methodName)
    {
        if (!string.IsNullOrEmpty(methodName) &&
            methodsByName.TryGetValue(
                methodName,
                out CityMethod method
            ))
        {
            RenderAllCallsForMethod(method);
        }
        else
        {
            Debug.LogWarning(
                "Method not found: " + methodName,
                this
            );
        }
    }

    public void RenderCallsForLastSelected()
    {
        if (currentlySeclected != null)
        {
            RenderCallsForMethodName(currentlySeclected.FullName);
        }
    }

    public void ShowSolutionForLastSelected()
    {
        if (currentlySeclected == null ||
            !methodsByName.TryGetValue(
                currentlySeclected.FullName,
                out CityMethod selected
            ))
        {
            return;
        }

        var selector =
            selected.gameObject.GetComponentInChildren<SelectTest>(true);

        if (selector != null)
        {
            selector.ShowSolution();
        }
    }

    public void RenderCallsForMethodRecursively(
        CityMethod method,
        int hops
    )
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);

        RenderCallsRecursive(method, hops, visited);
    }

    private void RenderCallsRecursive(
        CityMethod method,
        int hops,
        HashSet<string> visited
    )
    {
        if (method == null ||
            hops >= maxHops ||
            !visited.Add(method.name))
        {
            return;
        }

        RenderAllCallsForMethod(method);

        foreach (
            string name in method.outgoingCalls
                .Concat(method.incomingCalls)
                .Distinct()
        )
        {
            if (methodsByName.TryGetValue(
                name,
                out CityMethod next
            ))
            {
                RenderCallsRecursive(next, hops + 1, visited);
            }
        }
    }

    private GameObject RenderCube(
        int loc,
        string prefix,
        string name,
        string fullName,
        Color color,
        Vector3 position,
        Vector3 sizeScaling,
        GameObject parent,
        bool active = true,
        BugInstance[] bugs = null,
        bool withLOD = false
    )
    {
        GameObject classContainer = new GameObject();
        classContainer.name = $"{prefix}-{name}";

        GameObject clsBuilding = Instantiate(
            buildingPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        clsBuilding.SetActive(active);

        clsBuilding.GetComponent<SelectTest>().method =
            new CubeInfo(name, fullName, loc);

        clsBuilding.GetComponent<SelectTest>().bugs =
            bugs ?? new BugInstance[0];

        clsBuilding.GetComponent<Renderer>().material.color = color;

        var lodGroup = clsBuilding.GetComponent<LODGroup>();

        if (lodGroup != null)
        {
            lodGroup.enabled = withLOD;
        }

        clsBuilding.transform.parent = classContainer.transform;
        clsBuilding.transform.localPosition = new Vector3(
            0.5f,
            0,
            0.5f
        );

        if (active)
        {
            childMeshes.Add(clsBuilding);
        }

        classContainer.transform.position = position;
        classContainer.transform.localScale = sizeScaling;
        classContainer.transform.parent = parent.transform;

        return classContainer;
    }

    public TextAsset JsonFile
    {
        get
        {
            return jsonFile;
        }
        set
        {
            jsonFile = value;
        }
    }

    public TextAsset XmlFile
    {
        get
        {
            return xmlFile;
        }
        set
        {
            xmlFile = value;
        }
    }

    public TextAsset PackageAnalysisJson
    {
        get
        {
            return packageAnalysisJson;
        }
        set
        {
            packageAnalysisJson = value;
        }
    }
}