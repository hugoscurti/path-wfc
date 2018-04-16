using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;


public class AgentController : MonoBehaviour
{
    public GameObject prefabAgent;
    public int speed = 1;
    public GameObject map;

    private List<List<Vector3>> paths;

    private string[] _pathIndices = null;
    public string[] PathIndices { get { return _pathIndices; } }

    [HideInInspector]
    public int selectedPath;

    // The path the agent will follow
    private GameObject currentAgent;
    private List<Vector3> currentPath = null;
    private List<Vector3>.Enumerator currentTarget;

    public void ClearPaths()
    {
        paths = null;
        _pathIndices = null;
        currentPath = null;
        EditorApplication.update -= AgentUpdate;
        if (currentAgent != null)
            DestroyImmediate(currentAgent);
    }

    public void SetPaths(List<List<Vector3>> paths)
    {
        this.paths = paths;

        _pathIndices = Enumerable.Range(0, paths.Count).
            Select(i => i.ToString()).ToArray();

        if (_pathIndices.Length > 0)
            selectedPath = 0;
    }

    public void EnableAgent()
    {
        currentPath = paths[selectedPath];
        currentTarget = currentPath.GetEnumerator();
        if (!currentTarget.MoveNext())
            Debug.LogError("List is empty :(");

        if (currentAgent != null)
            currentAgent.GetComponent<Renderer>().enabled = true;
        else
            currentAgent = Instantiate(prefabAgent, map.transform, false);

        currentAgent.transform.localPosition = currentTarget.Current;

        // We assume here that paths are longer than 1
        currentTarget.MoveNext();

        // Start moving the agent
        EditorApplication.update += AgentUpdate;
    }

    public void AgentUpdate()
    {
        Debug.Log(Time.deltaTime);

        Vector3 direction = currentTarget.Current - currentAgent.transform.localPosition;
        float distance = direction.magnitude;
        direction.Normalize();

        float distToComplete = Time.deltaTime * speed;
        if (distToComplete > distance)
            distToComplete = distance;

        currentAgent.transform.localPosition += distToComplete * direction;

        // If arrived at destination
        if (Vector3.Distance(currentAgent.transform.localPosition, currentTarget.Current) < 1e-5)
        {
            // Resolve any misdirection due to floating error
            currentAgent.transform.localPosition = currentTarget.Current;

            if (!currentTarget.MoveNext())
            {
                // See if we must loop
                if (currentPath.First() == currentPath.Last())
                {
                    // Reset loop
                    currentTarget = currentPath.GetEnumerator();
                    currentTarget.MoveNext();   // First == last, we're already there
                    currentTarget.MoveNext();
                }
                else
                {
                    // Stop moving?
                    EditorApplication.update -= AgentUpdate;
                }
            }
        }
    }
}
