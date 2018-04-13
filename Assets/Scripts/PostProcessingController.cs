using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using Utilities;

public class PostProcessingController : MonoBehaviour
{
    [Serializable]
    public struct Containers
    {
        public GameObject map;
        public GameObject obstacles;
        public GameObject paths;
    }

    public PostProcessingAttributes attributes;
    public Containers containers;

    // Prefabs
    public GameObject obstacle;
    public GameObject agent;

    public Material lineMaterial;

    private List<LinkedList<int>> paths;
    private RectInt imageSize;
    private HashSet<int> obstacles;


    public void Clear()
    {
        containers.obstacles.transform.Clear();
        containers.paths.transform.Clear();
    }

    public void GenerateMap()
    {
        var overlapmodel = gameObject.GetComponent<PathOverlapController>().GetModel();

        paths = overlapmodel.GetPaths();
        obstacles = overlapmodel.GetObstacles();
        imageSize = overlapmodel.GetOutputRect();

        //Debug.Log(paths.Count);

        // 1. Set base map
        // baseMap is on the x,z plane
        // Plane is of size (10,10) when scaled by (1,1). Scale accordingly to fit the map
        Vector3 scale = new Vector3(imageSize.width / 10f, 1, imageSize.height / 10f);
        containers.map.transform.localScale = scale;

        // Put its position so that parent's origin is at 0,0 ?
        Vector3 pos = new Vector3(imageSize.width / 2f, 0, imageSize.height / 2f);
        containers.map.transform.localPosition = pos;

        // TODO: Merge cubes to form a giant mesh?

        int x, z;
        // 2. Set obstacles
        containers.obstacles.transform.Clear();
        foreach (int i in obstacles)
        {
            x = i % imageSize.width;
            z = i / imageSize.width;

            var cloneObstacle = Instantiate(obstacle, containers.obstacles.transform, false);
            cloneObstacle.transform.localPosition = new Vector3(x + 0.5f, 0.5f, z + 0.5f);
        }

        // 3. Show paths
        List<Vector3> wp_path;
        List<List<Vector3>> waypoint_paths = new List<List<Vector3>>();
        foreach (LinkedList<int> path in paths)
        {
            wp_path = new List<Vector3>();
            
            for(LinkedListNode<int> i = path.First; i != null; i = i.Next)
                wp_path.Add(new Vector3((i.Value % imageSize.width) + 0.5f, 0.5f, (i.Value / imageSize.width) + 0.5f));

            waypoint_paths.Add(wp_path);
        }

        // Render lines ?
        
        for (int i = 0; i < waypoint_paths.Count; ++i)
        {
            wp_path = waypoint_paths[i];
            GameObject pathobj = new GameObject($"Path {i}");
            pathobj.transform.parent = containers.paths.transform;
            pathobj.transform.localPosition = Vector3.zero;

            LineRenderer lr = pathobj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;

            lr.startWidth = lr.endWidth = 0.2f;
            lr.numCapVertices = 1;
            lr.material = lineMaterial;
            lr.startColor = lr.endColor = Color.black;
            lr.positionCount = wp_path.Count;
            lr.SetPositions(wp_path.ToArray());

            if (wp_path.Last() == wp_path.First())
                lr.loop = true;
        }


        // 4. Focus on map
        SceneView scene = SceneView.sceneViews[0] as SceneView;
        scene.in2DMode = false;
        scene.LookAt(containers.map.transform.position, containers.map.transform.rotation);
    }

}
