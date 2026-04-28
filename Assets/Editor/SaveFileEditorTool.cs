using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SaveFileEditorTool : EditorWindow
{
    private const string SaveFileName = "save.json";
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private Vector2 _scrollPosition;
    private string _prettyJson = "";
    private string _coloredJson = "";
    private string _statusMessage = "";
    private GUIStyle _jsonStyle;

    [MenuItem("Tools/Save File/Open Save File Viewer")]
    public static void OpenWindow()
    {
        var window = GetWindow<SaveFileEditorTool>("Save File Viewer");
        window.minSize = new Vector2(800, 500);
        window.RefreshContent();
        window.Show();
    }

    [MenuItem("Tools/Save File/Delete Save File")]
    private static void DeleteSaveFileMenu()
    {
        DeleteSaveFile();
    }

    [MenuItem("Tools/Save File/Log Save File Path")]
    private static void LogSaveFilePath()
    {
        Debug.Log($"Save file path: {SaveFilePath}");
    }

    [MenuItem("Tools/Save File/Open Persistent Data Folder")]
    private static void OpenPersistentDataFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    private void OnEnable()
    {
        CreateStyles();
        RefreshContent();
    }

    private void CreateStyles()
    {
        _jsonStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = false,
            alignment = TextAnchor.UpperLeft,
            font = Font.CreateDynamicFontFromOSFont("Consolas", 13)
        };

        if (_jsonStyle.font == null)
            _jsonStyle.font = EditorStyles.textArea.font;
    }

    private void OnGUI()
    {
        if (_jsonStyle == null)
            CreateStyles();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Save File Tools", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh", GUILayout.Height(28)))
            {
                RefreshContent();
            }

            if (GUILayout.Button("Delete Save File", GUILayout.Height(28)))
            {
                DeleteSaveFile();
                RefreshContent();
            }

            if (GUILayout.Button("Open Folder", GUILayout.Height(28)))
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            }

            if (GUILayout.Button("Log Path", GUILayout.Height(28)))
            {
                Debug.Log($"Save file path: {SaveFilePath}");
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Path", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(SaveFilePath, EditorStyles.textField, GUILayout.Height(18));

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        }

        EditorGUILayout.LabelField("Colorized JSON View", EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        Rect rect = GUILayoutUtility.GetRect(
            new GUIContent(_coloredJson),
            _jsonStyle,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));

        EditorGUI.DrawRect(rect, GetBackgroundColor());
        GUI.Label(rect, _coloredJson, _jsonStyle);

        EditorGUILayout.EndScrollView();
    }

    private void RefreshContent()
    {
        if (!File.Exists(SaveFilePath))
        {
            _statusMessage = "Save file not found.";
            _prettyJson = string.Empty;
            _coloredJson = string.Empty;
            Repaint();
            return;
        }

        try
        {
            string rawJson = File.ReadAllText(SaveFilePath);

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                _statusMessage = "Save file exists but is empty.";
                _prettyJson = string.Empty;
                _coloredJson = string.Empty;
            }
            else
            {
                _statusMessage = "Save file loaded successfully.";
                _prettyJson = PrettyPrintJson(rawJson);
                _coloredJson = ColorizeJson(_prettyJson);
            }
        }
        catch (System.Exception ex)
        {
            _statusMessage = "Failed to read save file.";
            _prettyJson = ex.ToString();
            _coloredJson = $"<color=#ff6b6b>{EscapeRichText(_prettyJson)}</color>";
        }

        Repaint();
    }

    private static void DeleteSaveFile()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Delete Save File",
            $"Are you sure you want to delete the save file?\n\n{SaveFilePath}",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log($"Save file deleted: {SaveFilePath}");
            EditorUtility.DisplayDialog("Success", "Save file deleted.", "OK");
        }
        else
        {
            Debug.LogWarning($"Save file not found: {SaveFilePath}");
            EditorUtility.DisplayDialog("Not Found", "Save file does not exist.", "OK");
        }
    }

    private static string PrettyPrintJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        var sb = new StringBuilder();
        bool inQuotes = false;
        bool escaping = false;
        int indent = 0;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (escaping)
            {
                sb.Append(c);
                escaping = false;
                continue;
            }

            if (c == '\\')
            {
                sb.Append(c);
                if (inQuotes)
                    escaping = true;
                continue;
            }

            if (c == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(c);
                continue;
            }

            if (inQuotes)
            {
                sb.Append(c);
                continue;
            }

            switch (c)
            {
                case '{':
                case '[':
                    sb.Append(c);
                    sb.AppendLine();
                    indent++;
                    sb.Append(new string(' ', indent * 4));
                    break;

                case '}':
                case ']':
                    sb.AppendLine();
                    indent--;
                    sb.Append(new string(' ', indent * 4));
                    sb.Append(c);
                    break;

                case ',':
                    sb.Append(c);
                    sb.AppendLine();
                    sb.Append(new string(' ', indent * 4));
                    break;

                case ':':
                    sb.Append(": ");
                    break;

                default:
                    if (!char.IsWhiteSpace(c))
                        sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static string ColorizeJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return string.Empty;

        string keyColor = EditorGUIUtility.isProSkin ? "#9CDCFE" : "#0451A5";
        string stringColor = EditorGUIUtility.isProSkin ? "#CE9178" : "#A31515";
        string numberColor = EditorGUIUtility.isProSkin ? "#B5CEA8" : "#098658";
        string boolColor = EditorGUIUtility.isProSkin ? "#569CD6" : "#0000FF";
        string nullColor = EditorGUIUtility.isProSkin ? "#569CD6" : "#0000FF";
        string punctuationColor = EditorGUIUtility.isProSkin ? "#D4D4D4" : "#333333";

        var sb = new StringBuilder();

        for (int i = 0; i < json.Length;)
        {
            char c = json[i];

            if (c == '"')
            {
                int start = i;
                i++;

                bool escaping = false;
                while (i < json.Length)
                {
                    char current = json[i];

                    if (escaping)
                    {
                        escaping = false;
                    }
                    else if (current == '\\')
                    {
                        escaping = true;
                    }
                    else if (current == '"')
                    {
                        i++;
                        break;
                    }

                    i++;
                }

                string token = json.Substring(start, i - start);

                int lookAhead = i;
                while (lookAhead < json.Length && char.IsWhiteSpace(json[lookAhead]))
                    lookAhead++;

                bool isKey = lookAhead < json.Length && json[lookAhead] == ':';
                string color = isKey ? keyColor : stringColor;

                sb.Append($"<color={color}>{EscapeRichText(token)}</color>");
                continue;
            }

            if (char.IsDigit(c) || c == '-')
            {
                int start = i;
                i++;

                while (i < json.Length &&
                       (char.IsDigit(json[i]) || json[i] == '.' || json[i] == 'e' || json[i] == 'E' || json[i] == '+' || json[i] == '-'))
                {
                    i++;
                }

                string token = json.Substring(start, i - start);
                sb.Append($"<color={numberColor}>{EscapeRichText(token)}</color>");
                continue;
            }

            if (StartsWith(json, i, "true"))
            {
                sb.Append($"<color={boolColor}>true</color>");
                i += 4;
                continue;
            }

            if (StartsWith(json, i, "false"))
            {
                sb.Append($"<color={boolColor}>false</color>");
                i += 5;
                continue;
            }

            if (StartsWith(json, i, "null"))
            {
                sb.Append($"<color={nullColor}>null</color>");
                i += 4;
                continue;
            }

            if ("{}[]:,".IndexOf(c) >= 0)
            {
                sb.Append($"<color={punctuationColor}>{EscapeRichText(c.ToString())}</color>");
                i++;
                continue;
            }

            sb.Append(EscapeRichText(c.ToString()));
            i++;
        }

        return sb.ToString();
    }

    private static bool StartsWith(string source, int index, string value)
    {
        if (index + value.Length > source.Length)
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (source[index + i] != value[i])
                return false;
        }

        return true;
    }

    private static string EscapeRichText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static Color GetBackgroundColor()
    {
        return EditorGUIUtility.isProSkin
            ? new Color(0.12f, 0.12f, 0.12f, 1f)
            : new Color(0.94f, 0.94f, 0.94f, 1f);
    }
}