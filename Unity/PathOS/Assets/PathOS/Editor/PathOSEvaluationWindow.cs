using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/*
PathOSEvaluationWindow.cs 
(Atiya Nova) 2021
 */

public enum HeuristicPriority
{
    NONE = 0,
    LOW = 1,
    MED = 2,
    HIGH = 3,
}

public enum HeuristicCategory
{
    NONE = 0,
    POS = 1,
    NEG = 2,
}

[Serializable]
class UserComment
{
    public string description = "";
    public bool categoryFoldout = false;
    public HeuristicPriority priority = HeuristicPriority.NONE;
    public HeuristicCategory category = HeuristicCategory.NONE;
}

class ExpertEvaluation
{
    //TODO: Spread things out in here to clean it up
    public List<UserComment> userComments = new List<UserComment>();
    private GUIStyle foldoutStyle = GUIStyle.none;

    private readonly string[] priorityNames = new string[] { "NA", "LOW", "MED", "HIGH" };
    private readonly string[] categoryNames = new string[] { "NA", "POS", "NEG" };
    private readonly string headerRow = "Number";
    private Color[] priorityColors = new Color[] { Color.white, Color.green, Color.yellow, new Color32(248, 114, 126, 255) };
    private Color[] categoryColors = new Color[] { Color.white, Color.green, new Color32(248, 114, 126, 255) };
    public void SaveData()
    {
        string saveName;

        saveName = "heuristicAmount";
        PlayerPrefs.SetInt(saveName, userComments.Count);

        for (int i = 0; i < userComments.Count; i++)
        {
            saveName = "heuristicsInputs " + i;
            PlayerPrefs.SetString(saveName, userComments[i].description);

            saveName = "heuristicsPriorities " + i;
            PlayerPrefs.SetInt(saveName, (int)userComments[i].priority);

            saveName = "heuristicsCategories " + i;
            PlayerPrefs.SetInt(saveName, (int)userComments[i].category);
        }
    }


    public void DrawHeuristics()
    {

        if (userComments.Count <= 0) return;

        EditorGUILayout.Space();

        foldoutStyle = EditorStyles.foldout;
        foldoutStyle.fontSize = 13;

        //girl what is this
        for (int i = 0; i < userComments.Count; i++)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("Toolbar");
            foldoutStyle.fontStyle = FontStyle.Bold;
            userComments[i].categoryFoldout = EditorGUILayout.Foldout(userComments[i].categoryFoldout, "Comment #" + i, foldoutStyle);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            if (!userComments[i].categoryFoldout) continue;

            EditorGUILayout.LabelField("Description", GUILayout.MaxWidth(Screen.width * 0.7f));
            EditorGUILayout.BeginHorizontal();
            EditorStyles.label.wordWrap = true;
            userComments[i].description = EditorGUILayout.TextArea(userComments[i].description, GUILayout.Width(Screen.width * 0.7f));
            GUI.backgroundColor = priorityColors[((int)userComments[i].priority)];
            userComments[i].priority = (HeuristicPriority)EditorGUILayout.Popup((int)userComments[i].priority, priorityNames);
            GUI.backgroundColor = categoryColors[((int)userComments[i].category)];
            userComments[i].category = (HeuristicCategory)EditorGUILayout.Popup((int)userComments[i].category, categoryNames);
            GUI.backgroundColor = priorityColors[0];
            EditorGUILayout.EndHorizontal();
        }
    }

    public void LoadData()
    {
        string saveName;
        int counter = 0;

        saveName = "heuristicAmount";
        if (PlayerPrefs.HasKey(saveName))
            counter = PlayerPrefs.GetInt(saveName);

        Debug.Log("this gets called");

        for (int i = 0; i < counter; i++)
        {
            userComments.Add(new UserComment());

            saveName = "heuristicsInputs " + i;
            if (PlayerPrefs.HasKey(saveName))
                userComments[i].description = PlayerPrefs.GetString(saveName);

            Debug.Log(saveName);

            saveName = "heuristicsPriorities " + i;
            if (PlayerPrefs.HasKey(saveName))
                userComments[i].priority = (HeuristicPriority)PlayerPrefs.GetInt(saveName);

            Debug.Log(saveName);

            saveName = "heuristicsCategories " + i;
            if (PlayerPrefs.HasKey(saveName))
                userComments[i].category = (HeuristicCategory)PlayerPrefs.GetInt(saveName);
        }
    }

    public void LoadHeuristics(string filename)
    {
        ImportHeuristics(filename);

        string saveName;

        for (int i = 0; i < userComments.Count; i++)
        {
            saveName = "heuristicsInputs " + i;
            if (PlayerPrefs.HasKey(saveName))
                userComments[i].description = PlayerPrefs.GetString(saveName);

            saveName = "heuristicsPriorities " + i;
            if (PlayerPrefs.HasKey(saveName))
                userComments[i].priority = (HeuristicPriority)PlayerPrefs.GetInt(saveName);

            saveName = "heuristicsCategories " + i;
            if (PlayerPrefs.HasKey(saveName))
                userComments[i].category = (HeuristicCategory)PlayerPrefs.GetInt(saveName);
        }

        //for (int t = 0; t < userComments.Count; t++)
        //{
        //    for (int j = 0; j < userComments[t].subcategories.Count; j++)
        //    {
        //        for (int i = 0; i < userComments[t].subcategories[j].heuristics.Count; i++)
        //        {
        //            filename = heuristicName + " heuristicsInputs " + t + " " + j + " " + i;
        //            if (PlayerPrefs.HasKey(filename))
        //                userComments[t].subcategories[j].heuristicInputs[i] = PlayerPrefs.GetString(filename);
        //
        //            filename = heuristicName + " heuristicsPriorities " + t + " " + j + " " + i;
        //            if (PlayerPrefs.HasKey(filename))
        //                userComments[t].subcategories[j].priorities[i] = (HeuristicPriority)PlayerPrefs.GetInt(filename);
        //        }
        //    }
        //}
    }

    public void ClearCurrentInputs()
    {
        for (int i = 0; i < userComments.Count; i++)
        {
            userComments[i].description = " ";
            userComments[i].priority = HeuristicPriority.NONE;
            userComments[i].category = HeuristicCategory.NONE;

            //for (int j = 0; j < userComments[t].subcategories.Count; j++)
            //{
            //    for (int i = 0; i < userComments[t].subcategories[j].heuristics.Count; i++)
            //    {
            //        userComments[t].subcategories[j].heuristicInputs[i] = " ";
            //        userComments[t].subcategories[j].priorities[i] = HeuristicPriority.NONE;
            //    }
            //}
        }

        SaveData();
    }

    public void ImportInputs(string filename)
    {
        StreamReader reader = new StreamReader(filename);

        string line = "";
        string[] lineContents;

        int inputCounter = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineContents = line.Split(',');

            if (lineContents.Length < 1)
            {
                Debug.Log("Error! Unable to read line");
                continue;
            }

            if (lineContents[0] == headerRow)
            {
                continue;
            }

            string newDescription = lineContents[1].Replace("  ", "\n").Replace("/", ",");
            userComments[inputCounter].description = newDescription;

            switch (lineContents[2])
            {
                case "NA":
                    userComments[inputCounter].priority = HeuristicPriority.NONE;
                    break;
                case "LOW":
                    userComments[inputCounter].priority = HeuristicPriority.LOW;
                    break;
                case "MED":
                    userComments[inputCounter].priority = HeuristicPriority.MED;
                    break;
                case "HIGH":
                    userComments[inputCounter].priority = HeuristicPriority.HIGH;
                    break;
            }

            switch (lineContents[4])
            {
                case "NA":
                    userComments[inputCounter].category = HeuristicCategory.NONE;
                    break;
                case "LOW":
                    userComments[inputCounter].category = HeuristicCategory.POS;
                    break;
                case "MED":
                    userComments[inputCounter].category = HeuristicCategory.NEG;
                    break;
            }

            inputCounter++;
        }
    }

    public void ImportHeuristics(string filename)
    {
        StreamReader reader = new StreamReader(filename);

        string line = "";
        string[] lineContents;
        int counter = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineContents = line.Split(',');

            if (lineContents.Length < 1)
            {
                Debug.Log("Error! Unable to read line");
                continue;
            }

            if (lineContents[0] == headerRow)
            {
                continue;
            }

            userComments.Add(new UserComment());

            userComments[counter].description = lineContents[1];

            switch (lineContents[2])
            {
                case "NA":
                    userComments[counter].priority = HeuristicPriority.NONE;
                    break;
                case "LOW":
                    userComments[counter].priority = HeuristicPriority.LOW;
                    break;
                case "MED":
                    userComments[counter].priority = HeuristicPriority.MED;
                    break;
                case "HIGH":
                    userComments[counter].priority = HeuristicPriority.HIGH;
                    break;
            }

            switch (lineContents[3])
            {
                case "NA":
                    userComments[counter].category = HeuristicCategory.NONE;
                    break;
                case "POS":
                    userComments[counter].category = HeuristicCategory.POS;
                    break;
                case "NEG":
                    userComments[counter].category = HeuristicCategory.NEG;
                    break;
            }

        }

        reader.Close();

    }
    public void ExportHeuristics(string filename)
    {
        StreamWriter writer = new StreamWriter(filename);

        writer.WriteLine("#, Description, Input, Priority, Category");
        string description, priority, category;

        for (int i = 0; i < userComments.Count; i++)
        {
            description = userComments[i].description.Replace("\r", "").Replace("\n", "  ").Replace(",", "/");

            switch (userComments[i].priority)
            {
                case HeuristicPriority.NONE:
                    priority = "NA";
                    break;
                case HeuristicPriority.LOW:
                    priority = "LOW";
                    break;
                case HeuristicPriority.MED:
                    priority = "MED";
                    break;
                case HeuristicPriority.HIGH:
                    priority = "HIGH";
                    break;
                default:
                    priority = "NA";
                    break;
            }

            switch (userComments[i].category)
            {
                case HeuristicCategory.NONE:
                    category = "NA";
                    break;
                case HeuristicCategory.POS:
                    category = "POS";
                    break;
                case HeuristicCategory.NEG:
                    category = "NEG";
                    break;
                default:
                    category = "NA";
                    break;
            }

            writer.WriteLine(i + ',' + description + ',' + priority + ',' + category);
        }

        writer.Close();
    }

    public void AddComment()
    {
        userComments.Add(new UserComment());
    }

}

[Serializable]
public class PathOSEvaluationWindow : EditorWindow
{
    private Color bgColor, btnColor;

    //Dropdown enums
    //enum DropdownOptions
    //{
    //    NONE = 0,
    //    PLAY_HEURISTICS = 1,
    //    NIELSEN = 2,
    //}

    // public string[] dropdownStrings = new string[] { "NONE", "PLAY HEURISTICS", "NIELSEN" };
    // public string[] templateLocations = new string[] { "Assets\\PathOS\\Settings\\PLAY_HEURISTICS_TEMPLATE.csv", "Assets\\PathOS\\Settings\\NIELSEN_TEMPLATE.csv" };
    // public string[] heuristicNames = new string[] { "PLAY", "NIELSEN" };

    // static DropdownOptions dropdowns = DropdownOptions.NONE;
    // ExpertEvaluation[] heuristics = new ExpertEvaluation[2];
    ExpertEvaluation heuristics = new ExpertEvaluation();
    //static int selected = 0;
    private void OnEnable()
    {
        //Background color
        bgColor = GUI.backgroundColor;
        btnColor = new Color32(200, 203, 224, 255);
       // heuristics.LoadData();
        ////Setting heuristic names
        //for (int i = 0; i < heuristics.Length; i++)
        //{
        //    heuristics[i] = new HeuristicGuideline();
        //    heuristics[i].heuristicName = heuristicNames[i];
        //}
        //
        ////Loading saved data
        //
        //if (PlayerPrefs.HasKey("selected"))
        //{
        //    selected = PlayerPrefs.GetInt("selected");
        //}
        //

    }

    private void OnDestroy()
    {
        //PlayerPrefs.SetInt("selected", selected);
        heuristics.SaveData();

    }

    private void OnDisable()
    {
        //PlayerPrefs.SetInt("selected", selected);
        heuristics.SaveData();
    }

    public void OnWindowOpen()
    {
        GUILayout.BeginHorizontal();
        //EditorGUIUtility.labelWidth = 70.0f;
        //GUILayout.BeginHorizontal();
        //
        //selected = EditorGUILayout.Popup("Heuristics:", selected, dropdownStrings);
        //GUILayout.EndHorizontal();
        //
        //if (selected > 0)
        //{
        GUI.backgroundColor = btnColor;


        if (GUILayout.Button("ADD"))
        {
            heuristics.AddComment();
        }

        if (GUILayout.Button("CLEAR"))
        {
            heuristics.ClearCurrentInputs();
        }

        if (GUILayout.Button("IMPORT"))
        {
            string importPath = EditorUtility.OpenFilePanel("Import Evaluation", "ASSETS\\EvaluationFiles", "csv");

            if (importPath.Length != 0)
            {
                heuristics.ImportInputs(importPath);
            }
        }

        if (GUILayout.Button("EXPORT"))
        {
            string importPath = EditorUtility.OpenFilePanel("Import Evaluation", "ASSETS\\EvaluationFiles", "csv");

            if (importPath.Length != 0)
            {
                heuristics.ExportHeuristics(importPath);
            }
        }

        GUI.backgroundColor = bgColor;
        //}
        //
        GUILayout.EndHorizontal();
        //
        //if (dropdowns != (DropdownOptions)selected)
        //{
        //    dropdowns = (DropdownOptions)selected;
        //
        //    if (dropdowns == DropdownOptions.NONE)
        //    {
        //        return;
        //    }
        //
        //    heuristics[selected - 1].loadedCategories.Clear();
        //    heuristics[selected - 1].LoadHeuristics(templateLocations[selected - 1]);
        //}
        //
        //if (selected <= 0)
        //{
        //    return;
        //}
        //
        heuristics.DrawHeuristics();
        heuristics.SaveData();
    }
}