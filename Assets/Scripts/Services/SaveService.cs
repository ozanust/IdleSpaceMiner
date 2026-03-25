using System.IO;
using UnityEngine;

/// <summary>
/// Persists and retrieves SaveData as JSON using Application.persistentDataPath.
/// </summary>
public class SaveService : ISaveService
{
    private const string SaveFileName = "save.json";

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    /// <inheritdoc/>
    public bool HasSave()
    {
        return File.Exists(SaveFilePath);
    }

    /// <inheritdoc/>
    public void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: false);
            File.WriteAllText(SaveFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveService] Failed to write save file: {e.Message}");
        }
    }

    /// <inheritdoc/>
    public SaveData Load()
    {
        if (!HasSave())
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveService] Failed to read save file: {e.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public void DeleteSave()
    {
        if (HasSave())
        {
            File.Delete(SaveFilePath);
        }
    }
}
