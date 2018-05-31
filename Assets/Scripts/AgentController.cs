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
    public string[] PathIndices { get; private set; } = null;

    [HideInInspector]
    public int selectedPath;

    // The path the agent will follow
    private GameObject currentAgent;
    private List<Vector3> currentPath = null;
    private List<Vector3>.Enumerator currentTarget;

    public void ClearPaths()
    {
        paths = null;
        PathIndices = null;
        currentPath = null;
        EditorApplication.update -= AgentUpdate;
        if (currentAgent != null)
            DestroyImmediate(currentAgent);
    }

    public void SetPaths(List<List<Vector3>> paths)
    {
        this.paths = paths;

        PathIndices = Enumerable.Range(0, paths.Count).
            Select(i => i.ToString()).ToArray();

        if (PathIndices.Length > 0)
            selectedPath = 0;
    }

    public void EnableAgent()
    {
        //Stop moving the agent
        EditorApplication.update -= AgentUpdate;

        currentPath = paths[selectedPath];
        currentTarget = currentPath.GetEnumerator();
        if (!currentTarget.MoveNext())
            Debug.LogError("List is empty :(");

        if (currentAgent != null)
            currentAgent.SetActive(true);
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
        Vector3 direction;
        float distance;

        float distToComplete = Time.deltaTime * speed;
        while (distToComplete > 0)
        {
            direction = currentTarget.Current - currentAgent.transform.localPosition;
            distance = direction.magnitude;
            direction.Normalize();

            if (distance > distToComplete)
                distance = distToComplete;

            // Move
            currentAgent.transform.localPosition += distance * direction;

            // If arrived at destination
            if (currentAgent.transform.localPosition == currentTarget.Current)
            {
                if (!GetNextDestination())
                {
                    EditorApplication.update -= AgentUpdate;
                    return;
                }
            }

            // Update distance to complete
            distToComplete -= distance;
        }
    }

    private bool GetNextDestination()
    {
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
                return false;
            }
        }

        return true;
    }
}
