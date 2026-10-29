using System;
using System.Collections.Generic;
using System.Linq;

public static class ScenarioValidator
{
    public static void Validate(PackageAnalysisResult result)
    {
        Require(result != null, "Scenario is null.");
        Require(!string.IsNullOrWhiteSpace(result.projectName), "projectName is missing.");
        Require(result.packages != null && result.packages.Length > 0, "Scenario has no packages.");
        var packageNames = new HashSet<string>();
        var classNames = new HashSet<string>();
        var methodNames = new HashSet<string>();
        var redPackages = new HashSet<string>();
        var yellowPackages = new HashSet<string>();
        int red = 0;
        int yellow = 0;
        foreach (var pkg in result.packages)
        {
            Require(pkg != null && !string.IsNullOrWhiteSpace(pkg.name), "Invalid package.");
            Require(packageNames.Add(pkg.name), "Duplicate package: " + pkg.name);
            Require(pkg.classes != null && pkg.classes.Length > 0, "Package has no classes: " + pkg.name);
            foreach (var cls in pkg.classes)
            {
                Require(cls != null && !string.IsNullOrWhiteSpace(cls.name), "Invalid class in " + pkg.name);
                Require(cls.name.StartsWith(pkg.name + ".", StringComparison.Ordinal),
                    "Class name must include its package: " + cls.name);
                Require(classNames.Add(cls.name), "Duplicate class: " + cls.name);
                Require(cls.startLine >= 1 && cls.endLine >= cls.startLine && cls.linesOfCode > 0 &&
                    cls.linesOfCode <= cls.endLine - cls.startLine + 1, "Invalid class metrics: " + cls.name);
                Require(cls.methods != null && cls.methods.Length > 0, "Class has no methods: " + cls.name);
                int methodLoc = 0;
                var ranges = new List<Tuple<int, int>>();
                foreach (var m in cls.methods)
                {
                    Require(m != null && !string.IsNullOrWhiteSpace(m.name), "Invalid method in " + cls.name);
                    Require(methodNames.Add(cls.name + "." + m.name),
                        "Duplicate/overloaded method names are not supported in this scenario: " + cls.name + "." + m.name);
                    Require(m.startLine >= cls.startLine && m.endLine >= m.startLine && m.endLine <= cls.endLine &&
                        m.linesOfCode > 0 && m.linesOfCode <= m.endLine - m.startLine + 1,
                        "Invalid method metrics: " + cls.name + "." + m.name);
                    Require(!ranges.Any(r => m.startLine <= r.Item2 && m.endLine >= r.Item1),
                        "Overlapping method ranges in " + cls.name);
                    ranges.Add(Tuple.Create(m.startLine, m.endLine));
                    methodLoc += m.linesOfCode;
                    Require(m.severity >= 0 && m.severity <= 2, "Unknown severity.");
                    if (m.severity > 0)
                    {
                        Require(!string.IsNullOrWhiteSpace(m.vulnerableCode) &&
                            !string.IsNullOrWhiteSpace(m.findingTitle), "Marked method needs code and title: " + cls.name);
                        Require(!string.IsNullOrWhiteSpace(m.fixedCode) && !string.IsNullOrWhiteSpace(m.explanation),
                            "Marked method needs a correction and explanation: " + cls.name + "." + m.name);
                        Require(m.vulnerableCode.Split('\n').Length <= m.endLine - m.startLine + 1,
                            "Code exceeds declared method range: " + cls.name);
                    }
                    if (m.severity == 1)
                    {
                        red++;
                        redPackages.Add(pkg.name);
                        Require(!string.IsNullOrWhiteSpace(m.fixedCode) && !string.IsNullOrWhiteSpace(m.explanation),
                            "Red method needs a fix and an explanation.");
                    }
                    if (m.severity == 2) { yellow++; yellowPackages.Add(pkg.name); }
                }
                Require(methodLoc <= cls.linesOfCode, "Method LOC exceed class LOC: " + cls.name);
            }
            Require(pkg.linesOfCode == pkg.classes.Sum(c => c.linesOfCode),
                "Package LOC must equal sum of class LOC: " + pkg.name);
        }
        Require(red == 1, "Exactly one red method is required.");
        Require(yellow >= 2 && yellow <= 5 && yellowPackages.Count == yellow,
            "Require 2 to 5 yellow methods in distinct packages.");
        Require(!redPackages.Overlaps(yellowPackages), "Red and yellow markers must be in different packages.");
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new ArgumentException(message);
    }
}
