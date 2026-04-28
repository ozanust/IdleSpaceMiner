/// <summary>
/// Provides save and load operations for the full game state.
/// </summary>
public interface ISaveService
{
    /// <summary>Returns true when a persisted save file exists on disk.</summary>
    bool HasSave();

    /// <summary>Writes the provided SaveData to disk.</summary>
    void Save(SaveData data);

    /// <summary>
    /// Loads and returns the persisted SaveData.
    /// Returns null if no save file exists or it cannot be read.
    /// </summary>
    SaveData Load();

    /// <summary>Deletes the save file from disk.</summary>
    void DeleteSave();
}
