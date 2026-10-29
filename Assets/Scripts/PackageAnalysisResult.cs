using System;

[Serializable]
public class PackageAnalysisResult
{
    public string projectName;
    public string scenarioDescription;
    public string lessonTitle;
    public string introduction;
    public string cityIntroduction;
    public PackageInfo[] packages;
}

[Serializable]
public class TreeMethodInfo
{
    public string name;
    public string signature;
}

[Serializable]
public class MethodInfo
{
    public string name;
    public int linesOfCode;
    public int startLine;
    public int endLine;
    public string[] callsFrom;
    public string[] callsTo;
    public int severity; // 0 = none, 1 = red, 2 = yellow
    public string findingTitle;
    public string vulnerableCode;
    public string fixedCode;
    public string explanation;
}

[Serializable]
public class ClassInfo
{
    public string name;
    public MethodInfo[] methods;
    public int linesOfCode;
    public int startLine;
    public int endLine;
}

[Serializable]
public class PackageInfo
{
    public string name;
    public ClassInfo[] classes;
    public int linesOfCode;
}
