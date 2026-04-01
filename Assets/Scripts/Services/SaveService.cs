using System.IO;
using UnityEngine;
public class SaveService : ISaveService
{
    private const string SaveFileName = "save.json";

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public bool HasSave()
    {
        return File.Exists(SaveFilePath);
    }

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

    public void DeleteSave()
    {
        if (HasSave())
        {
            File.Delete(SaveFilePath);
        }
    }
}
