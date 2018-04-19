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
    public PathOverlapController pathOverlapController;

    public Material lineMaterial;

    private RectInt imageSize;
    private HashSet<int> obstacles;


    public void Clear()
    {
        containers.obstacles.transform.Clear();
        containers.paths.transform.Clear();
        GetComponent<AgentController>().ClearPaths();
    }


    public void GenerateMap()
    {
        var overlapmodel = pathOverlapController.GetModel();

        var paths = overlapmodel.GetPaths();
        obstacles = overlapmodel.GetObstacles();
        imageSize = overlapmodel.GetOutputRect();

        InitalizeMap();

        // Remove smaller paths
        RemoveSmallerPaths(paths);

        // 3. Generate vector3 lists for maps
        var waypoint_paths = GenerateWaypointPaths(paths);

        if (attributes.SmoothPath)
        {
            foreach (List<Vector3> path in waypoint_paths)
            {
                SimplifyPaths(path, attributes.SmoothTolerance);
                SmoothenCorners(path, attributes.ChaikinIterations);
            }
        }
        
        

        // Render paths
        RenderLines(waypoint_paths);

        // Put paths in AgentController
        GetComponent<AgentController>().SetPaths(waypoint_paths);

        // 4. Focus on map
        //SceneView scene = SceneView.sceneViews[0] as SceneView;
        //scene.in2DMode = false;
        //scene.LookAt(containers.map.transform.position, Quaternion.Euler(50, 0, 0));
        //scene.camera.fieldOfView = 60;
    }

    public void InitalizeMap()
    {
        // 1. Set base map
        // baseMap is on the x,z plane
        // Plane is of size (10,10) when scaled by (1,1). Scale accordingly to fit the map
        Vector3 scale = new Vector3(imageSize.width / 10f, 1, imageSize.height / 10f);
        containers.map.transform.localScale = scale;

        // Put its position so that parent's origin is at 0,0
        Vector3 pos = new Vector3(imageSize.width / 2f, 0, imageSize.height / 2f);
        containers.map.transform.localPosition = pos;

        // 2. Create obstacles
        CreateObstacles();
    }

    public void RemoveSmallerPaths(List<LinkedList<int>> paths)
    {
        var removed = paths.RemoveAll(path => path.Count - 1 < attributes.MinPathLengh);
        Debug.Log($"Removed {removed} paths.");
    }

    public void SimplifyPaths(List<Vector3> path, float tolerance)
    {
        //Ramer-Douglas-Peucker for path simplification
        List<Vector2> points = new List<Vector2>();
        List<int> keepPoints = new List<int>();
        foreach (Vector3 p in path)
            points.Add(new Vector2(p.x, p.z));

        // Simplify implements Ramer-Douglas-Peucker!
        LineUtility.Simplify(points, tolerance, keepPoints);

        List<Vector3> origPath = new List<Vector3>(path);
        path.Clear();
        foreach (int p in keepPoints)
            path.Add(origPath[p]);
    }

    public void SmoothenCorners(List<Vector3> path, int levels)
    {
        bool isLoop = path.First() == path.Last();
        if (levels == 0) return;

        Vector3 qi, ri;

        List<Vector3> res = new List<Vector3>(path);
        List<Vector3> lprev = path, lcurr = res;

        while(levels-- > 0)
        {
            lcurr.Clear();
            for(int i = 0; i < lprev.Count - 1; ++i)
            {
                qi = 0.75f * lprev[i] + 0.25f * lprev[i + 1];
                ri = 0.25f * lprev[i] + 0.75f * lprev[i + 1];
                lcurr.Add(qi);
                lcurr.Add(ri);
            }

            // Close loop if it's a loop
            if (isLoop)
                lcurr.Add(lcurr[0]);

            lprev = lcurr;
            lcurr = (lcurr == res ? path : res);
        }

        // Put result in path
        if (lprev != path)
        {
            path.Clear();
            path.AddRange(lprev);
        }
    }

    public void CreateObstacles()
    {
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

            // Check for neighbours
            //int di = (z * imageSize.width) + (x - 1);

            //if (obstacles.Contains(di))
            //{
            //    var mesh = cloneObstacle.GetComponent<MeshFilter>().sharedMesh;
            //}
        }
    }

    /// <summary>
    /// Generate paths of vector3 points from the initial list of paths
    /// </summary>
    public List<List<Vector3>> GenerateWaypointPaths(List<LinkedList<int>> paths)
    {
        List<List<Vector3>> waypoint_paths = new List<List<Vector3>>();
        foreach (LinkedList<int> path in paths)
        {
            var wp_path = new List<Vector3>();

            for (LinkedListNode<int> i = path.First; i != null; i = i.Next)
                wp_path.Add(new Vector3((i.Value % imageSize.width) + 0.5f, 0.5f, (i.Value / imageSize.width) + 0.5f));

            waypoint_paths.Add(wp_path);
        }

        return waypoint_paths;
    }

    public void RenderLines(List<List<Vector3>> waypoint_paths)
    {
        // Render lines
        for (int i = 0; i < waypoint_paths.Count; ++i)
        {
            var wp_path = waypoint_paths[i];
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
    }

    public void CombineMeshes()
    {
        MeshFilter[] meshes = containers.obstacles.GetComponentsInChildren<MeshFilter>(true).
            Where(c => c.transform.parent == containers.obstacles.transform).ToArray();

        CombineInstance[] combine = new CombineInstance[meshes.Length];
        Matrix4x4 wtlParent = containers.obstacles.transform.worldToLocalMatrix;

        int i = 0;
        while (i < meshes.Length)
        {
            combine[i].mesh = meshes[i].sharedMesh;
            combine[i].transform = wtlParent * meshes[i].transform.localToWorldMatrix;
            meshes[i].gameObject.SetActive(false);
            ++i;
        }

        MeshFilter combinedMesh = containers.obstacles.GetComponent<MeshFilter>();
        combinedMesh.sharedMesh = new Mesh();
        combinedMesh.sharedMesh.CombineMeshes(combine, true);

        MergeFaces(combinedMesh.sharedMesh);

        combinedMesh.gameObject.SetActive(true);

        // Remove faces that are adjacent to each other

    }

    public void MergeFaces(Mesh mesh)
    {
        int[] oldTris = mesh.triangles;
    }

}
