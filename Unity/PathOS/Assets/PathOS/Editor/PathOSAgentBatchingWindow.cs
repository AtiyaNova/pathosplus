﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/*
PathOSAgentBatchingWindow.cs 
PathOSAgentBatchingWindow (c) Nine Penguins (Samantha Stahlke) 2019
*/
public class PathOSAgentBatchingWindow : EditorWindow
{
    //Used to identify preferences string by Unity.
    private const string editorPrefsID = "PathOSAgentBatching";

    private const int pathDisplayLength = 32;
    private GUIStyle errorStyle = new GUIStyle();

    /* Basic Settings */
    [SerializeField]
    private PathOSAgent agentReference;

    [SerializeField]
    private bool hasAgent;

    [SerializeField]
    private int agentID;

    [SerializeField]
    private int numAgents;

    //Simulating multiple agents simultaneously.
    [SerializeField]
    private bool simultaneousProperty = false;
    private bool simultaneous = false;

    [SerializeField]
    private Vector3 startLocation;

    [SerializeField]
    private string loadPrefabFile = "--";

    private string shortPrefabFile;

    private bool validPrefabFile = false;

    [System.Serializable]
    private class RuntimeAgentReference
    {
        public PathOSAgent agent;
        public int instanceID;

        public RuntimeAgentReference(PathOSAgent agent)
        {
            instanceID = agent.GetInstanceID();
        }

        public void UpdateReference()
        {
            agent = EditorUtility.InstanceIDToObject(instanceID) as PathOSAgent;
        }
    }

    [SerializeField]
    private List<RuntimeAgentReference> instantiatedAgents = 
        new List<RuntimeAgentReference>();

    [SerializeField]
    private List<RuntimeAgentReference> existingSceneAgents = 
        new List<RuntimeAgentReference>();

    //Max number of agents to be simulated simultaneously.
    private const int MAX_AGENTS_SIMULTANEOUS = 8;

    [SerializeField]
    private float timeScale = 1.0f;

    public enum HeuristicMode
    {
        FIXED = 0,
        RANGE,
        LOAD
    };

    private string[] heuristicModeLabels =
    {
        "Fixed Values",
        "Random Within Range",
        "Load from File"
    };

    /* Behaviour Customization */
    [SerializeField]
    private HeuristicMode heuristicMode;

    private Dictionary<PathOS.Heuristic, string> heuristicLabels =
        new Dictionary<PathOS.Heuristic, string>();

    [SerializeField]
    private List<PathOS.HeuristicScale> fixedHeuristics =
        new List<PathOS.HeuristicScale>();

    private Dictionary<PathOS.Heuristic, float> fixedLookup =
        new Dictionary<PathOS.Heuristic, float>();

    [SerializeField]
    private float fixedExp;

    [SerializeField]
    private List<PathOS.HeuristicRange> rangeHeuristics =
        new List<PathOS.HeuristicRange>();

    private Dictionary<PathOS.Heuristic, PathOS.FloatRange> rangeLookup =
        new Dictionary<PathOS.Heuristic, PathOS.FloatRange>();

    [SerializeField]
    private PathOS.FloatRange rangeExp;

    //TODO: Implementation for loading a series of agents from a file.
    [SerializeField]
    private string loadHeuristicsFile = "--";

    private string shortHeuristicsFile;

    /* Simulation Controls */
    private bool simulationActive = false;
    private bool triggerFrame = false;
    private bool cleanupWait = false;
    private bool cleanupFrame = false;
    private bool wasPlaying = false;
    private int agentsLeft = 0;

    [MenuItem("Window/PathOS Agent Batching")]
    public static void ShowWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(PathOSAgentBatchingWindow), true, 
            "PathOS Agent Batching");

        window.minSize = new Vector2(420.0f, 690.0f);
    }

    private void OnEnable()
    {
        //Load saved settings.
        string prefsData = EditorPrefs.GetString(editorPrefsID, JsonUtility.ToJson(this, false));
        JsonUtility.FromJsonOverwrite(prefsData, this);

        //Re-establish agent reference, if it has been nullified.
        //This can happen when switching into Playmode.
        //Otherwise, re-grab the agent's instance ID.
        if (hasAgent)
        {
            if(agentReference != null)
                agentID = agentReference.GetInstanceID();
            else
                agentReference = EditorUtility.InstanceIDToObject(agentID) as PathOSAgent;
        }

        hasAgent = agentReference != null;

        //Build the heuristic lookups.
        foreach (PathOS.Heuristic heuristic in 
            System.Enum.GetValues(typeof(PathOS.Heuristic)))
        {
            fixedLookup.Add(heuristic, 0.0f);
            rangeLookup.Add(heuristic, new PathOS.FloatRange { min = 0.0f, max = 1.0f });
        }

        System.Array heuristics = System.Enum.GetValues(typeof(PathOS.Heuristic));

        //Check that we have the correct number of heuristics.
        //(Included to future-proof against changes to the list).
        if (fixedHeuristics.Count != heuristics.Length)
        {
            fixedHeuristics.Clear();
            foreach(PathOS.Heuristic heuristic in heuristics)
            {
                fixedHeuristics.Add(new PathOS.HeuristicScale(heuristic, 0.0f));
            }
        }

        if (rangeHeuristics.Count != heuristics.Length)
        {
            rangeHeuristics.Clear();
            foreach(PathOS.Heuristic heuristic in heuristics)
            {
                rangeHeuristics.Add(new PathOS.HeuristicRange(heuristic));
            }
        }

        foreach (PathOS.Heuristic heuristic in heuristics)
        {
            string label = heuristic.ToString();

            label = label.Substring(0, 1).ToUpper() + label.Substring(1).ToLower();
            heuristicLabels.Add(heuristic, label);
        }

        if (loadHeuristicsFile == "")
            loadHeuristicsFile = "--";

        if (loadPrefabFile == "")
            loadPrefabFile = "--";

        PathOS.UI.TruncateStringHead(loadHeuristicsFile,
            ref shortHeuristicsFile, pathDisplayLength);
        PathOS.UI.TruncateStringHead(loadPrefabFile, 
            ref shortPrefabFile, pathDisplayLength);

        errorStyle.normal.textColor = Color.red;
        CheckPrefabFile();

        Repaint();
    }

    private void OnDisable()
    {
        //Save settings to the editor.
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);     
    }

    private void OnDestroy()
    {
        //Reset the timescale.
        Time.timeScale = 1.0f;

        DeleteInstantiatedAgents(instantiatedAgents.Count);
        SetSceneAgentsActive(true);

        instantiatedAgents.Clear();
        existingSceneAgents.Clear();

        //Save settings to the editor.
        string prefsData = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString(editorPrefsID, prefsData);
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        GrabAgentReference();
        agentReference = EditorGUILayout.ObjectField("Agent Reference: ", agentReference, typeof(PathOSAgent), true)
            as PathOSAgent;

        //Update agent ID if the user has selected a new object reference.
        if(EditorGUI.EndChangeCheck())
        {
            hasAgent = agentReference != null;

            if (hasAgent)
                agentID = agentReference.GetInstanceID();        
        }

        numAgents = EditorGUILayout.IntField("Number of agents: ", numAgents);

        simultaneousProperty = EditorGUILayout.Toggle(
            "Simulate Simultaneously", simultaneousProperty);

        //If simultaneous simulation is selected, draw the prefab selection utility.
        if(simultaneousProperty)
        {
            startLocation = EditorGUILayout.Vector3Field("Starting location: ", startLocation);

            EditorGUILayout.LabelField("Prefab to use: ", shortPrefabFile);

            if (GUILayout.Button("Select Prefab..."))
            {
                loadPrefabFile = EditorUtility.OpenFilePanel("Select Prefab...",
                    Application.dataPath, "prefab");

                PathOS.UI.TruncateStringHead(loadPrefabFile, 
                    ref shortPrefabFile, pathDisplayLength);

                CheckPrefabFile();
            }

            if (!validPrefabFile)
            {
                EditorGUILayout.LabelField("Error! You must select a Unity prefab" +
                    " with the PathOSAgent component.", errorStyle);
            }

            if(GUILayout.Button("Test Instantiation"))
            {
                FindSceneAgents();
                SetSceneAgentsActive(false);
                InstantiateAgents(4);
            }

            if(GUILayout.Button("Test Removal"))
            {
                DeleteInstantiatedAgents(4);
                SetSceneAgentsActive(true);
            }

        }

        timeScale = EditorGUILayout.Slider("Timescale: ", timeScale, 1.0f, 8.0f);

        heuristicMode = (HeuristicMode)GUILayout.SelectionGrid(
            (int)heuristicMode, heuristicModeLabels, heuristicModeLabels.Length);

        switch(heuristicMode)
        {
            case HeuristicMode.FIXED:

                if (GUILayout.Button("Load from Agent"))
                    LoadHeuristicsFromAgent();

                fixedExp = EditorGUILayout.Slider("Experience Scale",
                    fixedExp, 0.0f, 1.0f);

                for (int i = 0; i < fixedHeuristics.Count; ++i)
                {
                    fixedHeuristics[i].scale = EditorGUILayout.Slider(
                        heuristicLabels[fixedHeuristics[i].heuristic],
                        fixedHeuristics[i].scale, 0.0f, 1.0f);
                }

                break;

            case HeuristicMode.RANGE:

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.MinMaxSlider("Experience Scale",
                    ref rangeExp.min, ref rangeExp.max, 0.0f, 1.0f);

                rangeExp.min = EditorGUILayout.FloatField(
                    PathOS.UI.RoundFloatfield(rangeExp.min), 
                    GUILayout.Width(PathOS.UI.shortFloatfieldWidth));

                EditorGUILayout.LabelField("<->", 
                    GUILayout.Width(PathOS.UI.shortLabelWidth));

                rangeExp.max = EditorGUILayout.FloatField(
                    PathOS.UI.RoundFloatfield(rangeExp.max), 
                    GUILayout.Width(PathOS.UI.shortFloatfieldWidth));

                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < rangeHeuristics.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.MinMaxSlider(
                        heuristicLabels[rangeHeuristics[i].heuristic],
                        ref rangeHeuristics[i].range.min,
                        ref rangeHeuristics[i].range.max,
                        0.0f, 1.0f);

                    rangeHeuristics[i].range.min = EditorGUILayout.FloatField(
                        PathOS.UI.RoundFloatfield(rangeHeuristics[i].range.min), 
                        GUILayout.Width(PathOS.UI.shortFloatfieldWidth));

                    EditorGUILayout.LabelField("<->", 
                        GUILayout.Width(PathOS.UI.shortLabelWidth));

                    rangeHeuristics[i].range.max = EditorGUILayout.FloatField(
                        PathOS.UI.RoundFloatfield(rangeHeuristics[i].range.max), 
                        GUILayout.Width(PathOS.UI.shortFloatfieldWidth));

                    EditorGUILayout.EndHorizontal();
                }

                break;

            case HeuristicMode.LOAD:

                EditorGUILayout.LabelField("File to load: ", shortHeuristicsFile);

                if (GUILayout.Button("Select CSV..."))
                {
                    loadHeuristicsFile = EditorUtility.OpenFilePanel("Select CSV...",
                        Application.dataPath, "csv");

                    PathOS.UI.TruncateStringHead(loadHeuristicsFile, 
                        ref shortHeuristicsFile, pathDisplayLength);
                }

                break;
        }

        //Apply new heuristic values to the agent.
        if (GUILayout.Button("Apply to agent"))
            ApplyHeuristics();

        if(GUILayout.Button("Start"))
        {
            if (PathOSManager.instance != null)
            {
                simultaneous = simultaneousProperty;
                simulationActive = true;
                agentsLeft = numAgents;

                if (simultaneous)
                {
                    FindSceneAgents();
                    SetSceneAgentsActive(false);
                }
            }
            else
                NPDebug.LogError("Can't start simulation without a " +
                    "PathOS manager in the scene!");               
        }

        if(GUILayout.Button("Stop"))
        {
            simulationActive = false;
            EditorApplication.isPlaying = false;
            cleanupWait = true;
        }        
    }

    private void Update()
    {
        if (simulationActive)
        {
            if (triggerFrame)
            {
                EditorApplication.isPlaying = true;
                Time.timeScale = timeScale;
                triggerFrame = false;
            }
            else if (!EditorApplication.isPlaying)
            {
                if (agentsLeft == 0 || (wasPlaying 
                    && !EditorPrefs.GetBool(
                        PathOSManager.simulationEndedEditorPrefsID)))
                {
                    agentsLeft = 0;
                    simulationActive = false;
                    cleanupFrame = true;
                }
                else
                {
                    if (simultaneous)
                    {
                        if (agentsLeft > instantiatedAgents.Count)
                        {
                            InstantiateAgents(Mathf.Min(
                                MAX_AGENTS_SIMULTANEOUS - instantiatedAgents.Count,
                                agentsLeft - instantiatedAgents.Count));
                        }
                        else if (agentsLeft < instantiatedAgents.Count)
                        {
                            DeleteInstantiatedAgents(instantiatedAgents.Count - agentsLeft);
                        }

                        ApplyHeuristicsInstantiated();
                        agentsLeft -= instantiatedAgents.Count;
                    }
                    else
                    {
                        ApplyHeuristics();
                        --agentsLeft;
                    }

                    //We need to wait one frame to ensure Unity
                    //saves the changes to agent heuristic values
                    //in the undo stack.
                    triggerFrame = true;
                }
            }
        }
        else if(cleanupWait)
        {
            cleanupWait = false;
            cleanupFrame = true;
        }
        else if(cleanupFrame)
        {
            cleanupFrame = false;
            cleanupWait = false;
            triggerFrame = false;

            if (simultaneous)
            {
                SetSceneAgentsActive(true);
                DeleteInstantiatedAgents(instantiatedAgents.Count);
            }
        }

        wasPlaying = EditorApplication.isPlaying;
    }

    //Grab fixed heuristic values from the agent reference specified.
    private void LoadHeuristicsFromAgent()
    {
        if(null == agentReference)
            return;

        foreach(PathOS.HeuristicScale scale in agentReference.heuristicScales)
        {
            fixedLookup[scale.heuristic] = scale.scale;
        }

        foreach(PathOS.HeuristicScale scale in fixedHeuristics)
        {
            scale.scale = fixedLookup[scale.heuristic];
        }

        fixedExp = agentReference.experienceScale;
    }

    private void SyncFixedLookup()
    {
        foreach(PathOS.HeuristicScale scale in fixedHeuristics)
        {
            fixedLookup[scale.heuristic] = scale.scale;
        }        
    }

    private void SyncRangeLookup()
    {
        foreach (PathOS.HeuristicRange range in rangeHeuristics)
        {
            rangeLookup[range.heuristic] = range.range;
        }
    }

    private void GrabAgentReference()
    {
        if(hasAgent && null == agentReference)
            agentReference = EditorUtility.InstanceIDToObject(agentID) as PathOSAgent;
    }

    private void ApplyHeuristics()
    {
        GrabAgentReference();

        if (null == agentReference)
            return;

        Undo.RecordObject(agentReference, "Set Agent Heuristics");

        SetHeuristics(agentReference);
    }

    private void SetHeuristics(PathOSAgent agent)
    {
        switch (heuristicMode)
        {
            case HeuristicMode.FIXED:

                SyncFixedLookup();

                foreach (PathOS.HeuristicScale scale in agent.heuristicScales)
                {
                    scale.scale = fixedLookup[scale.heuristic];
                }

                agent.experienceScale = fixedExp;
                break;

            case HeuristicMode.RANGE:

                SyncRangeLookup();

                foreach (PathOS.HeuristicScale scale in agent.heuristicScales)
                {
                    PathOS.FloatRange range = rangeLookup[scale.heuristic];
                    scale.scale = Random.Range(range.min, range.max);
                }

                agent.experienceScale = Random.Range(rangeExp.min, rangeExp.max);
                break;

            case HeuristicMode.LOAD:
                break;
        }
    }

    private void ApplyHeuristicsInstantiated()
    {
        for (int i = 0; i < instantiatedAgents.Count; ++i)
        {
            instantiatedAgents[i].UpdateReference();

            EditorUtility.SetDirty(instantiatedAgents[i].agent);
            SetHeuristics(instantiatedAgents[i].agent);
        }
    }

    private void CheckPrefabFile()
    {
        string loadPrefabFileLocal = GetLocalPrefabFile();
        validPrefabFile = AssetDatabase.LoadAssetAtPath<PathOSAgent>(loadPrefabFileLocal);
    }

    private string GetLocalPrefabFile()
    {
        if (loadPrefabFile.Length < Application.dataPath.Length)
            return "";

        //PrefabUtility needs paths relative to the project folder.
        //Application.dataPath gives us the project folder + "/Assets".
        //We need our string to start with "Assets".
        //Ergo, we split the string starting at the length of the data path - 6.
        return loadPrefabFile.Substring(Application.dataPath.Length - 6);
    }

    private void FindSceneAgents()
    {
        existingSceneAgents.Clear();

        foreach(PathOSAgent agent in FindObjectsOfType<PathOSAgent>())
        {
            existingSceneAgents.Add(new RuntimeAgentReference(agent));
        }
    }

    private void SetSceneAgentsActive(bool active)
    {
        for(int i = 0; i < existingSceneAgents.Count; ++i)
        {
            existingSceneAgents[i].UpdateReference();
            existingSceneAgents[i].agent.gameObject.SetActive(active);
            EditorUtility.SetDirty(existingSceneAgents[i].agent.gameObject);
        }
    }

    private void InstantiateAgents(int count)
    {
        if (!validPrefabFile)
            return;

        PathOSAgent prefab = AssetDatabase.LoadAssetAtPath<PathOSAgent>(GetLocalPrefabFile());

        if (null == prefab)
            return;

        for (int i = 0; i < count; ++i)
        {
            GameObject newAgent = PrefabUtility.InstantiatePrefab(prefab.gameObject) as GameObject;
            newAgent.transform.position = startLocation;
            newAgent.name = "Temporary Batch Agent " + 
                (instantiatedAgents.Count).ToString();

            instantiatedAgents.Add(new RuntimeAgentReference(
                newAgent.GetComponent<PathOSAgent>()));
        }
    }

    private void DeleteInstantiatedAgents(int count)
    {
        if (count > instantiatedAgents.Count)
            count = instantiatedAgents.Count;

        for (int i = 0; i < count; ++i)
        {      
            instantiatedAgents[instantiatedAgents.Count - 1].UpdateReference();

            if (instantiatedAgents[instantiatedAgents.Count - 1].agent)
                Object.DestroyImmediate(
                    instantiatedAgents[instantiatedAgents.Count - 1].agent.gameObject);

            instantiatedAgents.RemoveAt(instantiatedAgents.Count - 1);
        }
    }
}
