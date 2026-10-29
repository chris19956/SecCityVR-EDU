using System;
using System.IO;
using UnityEngine;

public static class LLMConfigLoader
{
    public static LLMConfig Load()
    {
        // No key is embedded in a scene or a shipped Resources asset.
        var config = RuntimeServiceConfig.Read<LLMConfig>("llm-config.json",
            "Scripts/LLM/Config/llm-config.json") ?? new LLMConfig();
        config.endpoint = RuntimeServiceConfig.EnvironmentOr("SECCITY_LLM_ENDPOINT", config.endpoint);
        config.model = RuntimeServiceConfig.EnvironmentOr("SECCITY_LLM_MODEL", config.model);
        config.apiKey = RuntimeServiceConfig.EnvironmentOr("SECCITY_LLM_API_KEY", config.apiKey);
        return config;
    }
}

public static class RuntimeServiceConfig
{
    public static string EnvironmentOr(string name, string fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
    public static T Read<T>(string fileName, string editorRelativePath) where T : class
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
#if UNITY_EDITOR
        if (!File.Exists(path)) path = Path.Combine(Application.dataPath, editorRelativePath);
#endif
        if (!File.Exists(path)) return null;
        try { return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
        catch (Exception exception)
        {
            Debug.LogWarning("Service configuration could not be read (" + fileName + "): " + exception.GetType().Name);
            return null;
        }
    }
}
