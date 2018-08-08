using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

using Utilities;

public class PostProcessingController : MonoBehaviour
{
    public PathOverlapController pathOverlapController;

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

    public Material lineMaterial;

    private Size imageSize;
    private HashSet<int> obstacles;

    private PostProcessingModel ppmodel = new PostProcessingModel();

    // Job variables
    JobHandle[] JobHandles;
    NativeArray<Vector3>[] PathArrays;
    NativeArray<int>[] PathCounts;
    NativeArray<int> NativeObstacles;
    bool[] HasBeenProcessed;

    List<List<Vector3>> waypoint_paths;


    // Timing variables
    long begin, end;

    public void Clear()
    {
        containers.obstacles.transform.Clear();
        containers.paths.transform.Clear();
        containers.map.transform.localScale = Vector3.zero;
        GetComponent<AgentController>().ClearPaths();
    }


    public void GenerateMap()
    {
        var overlapmodel = pathOverlapController.GetModel();

        ppmodel.Init(overlapmodel);
        var paths = ppmodel.GetPaths();
        obstacles = overlapmodel.GetObstacles();
        imageSize = overlapmodel.GetOutputRect();

        InitalizeMap();

        if (attributes.ApplyPostProcessing)
            // Remove smaller paths
            RemoveSmallerPaths(paths);

        // 3. Generate vector3 lists for maps
        waypoint_paths = GenerateWaypointPaths(paths);

        if (attributes.ApplyPostProcessing)
        {
            BeginSmoothingJob(waypoint_paths, attributes.Tolerance, attributes.Iterations);
        } else
        {
            RenderPaths(waypoint_paths);
            GetComponent<AgentController>().SetPaths(waypoint_paths);
        }

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

    public bool CrossObstacle(Vector2 p1, Vector2 p2)
    {
        // Add a small threshold to the Rect
        float threshold = 0.2f;

        // Ray's direction is normalized...
        Vector2 dist = p2 - p1;
        float maxDist = dist.magnitude;
        Ray2D ray = new Ray2D(p1, dist);
        Rect obst = new Rect();
        foreach (int i in obstacles)
        {
            /// Set obstacle
            obst.x = (i % imageSize.width) - threshold;
            obst.y = (i / imageSize.width) - threshold;
            obst.size =  Vector2.one * (1 + threshold * 2);

            // Set line
            if (ray.Intersect(obst, maxDist))
                return true;
        }

        return false;
    }

    public void RemoveSmallerPaths(List<LinkedList<int>> paths)
    {
        // We count paths length using edge. E.g. a path of 2 points is of size 1
        var removed = paths.RemoveAll(path => path.Count - 1 < attributes.MinPathLengh);
        Debug.Log($"Removed {removed} paths.");
    }


    void UpdateJobStatus()
    {
        bool allProcessed = true;

        for (int i = 0; i < waypoint_paths.Count; ++i)
        {
            if (!HasBeenProcessed[i])
            {
                if (JobHandles[i].IsCompleted)
                {
                    JobHandles[i].Complete();

                    waypoint_paths[i].Clear();

                    var path = waypoint_paths[i];
                    var pathCount = PathCounts[i];
                    var pathArray = PathArrays[i];

                    // Copy back to path
                    path.Clear();
                    for (int j = 0; j < pathCount[0]; ++j)
                        path.Add(pathArray[j]);
                    pathCount.Dispose();
                    pathArray.Dispose();

                    RenderPath(path, i);
                    HasBeenProcessed[i] = true;
                }
                else
                    allProcessed = false;
            }
        }

        if (allProcessed)
        {
            NativeObstacles.Dispose();

            // Put paths in AgentController
            GetComponent<AgentController>().SetPaths(waypoint_paths);

            EditorApplication.update -= UpdateJobStatus;

            // End timer
            end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Debug.Log($"Time: {(end - begin) / 1000f} sec.");
        }
    }

    public void BeginSmoothingJob(List<List<Vector3>> paths, float tolerance, int iterations)
    {
        // Begin timing
        begin = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        // Create shared obstacles
        NativeObstacles = new NativeArray<int>(obstacles.Count, Allocator.Persistent);
        int k = 0;
        foreach (int ob in obstacles)
            NativeObstacles[k++] = ob;


        JobHandles = new JobHandle[paths.Count];
        PathArrays = new NativeArray<Vector3>[paths.Count];
        PathCounts = new NativeArray<int>[paths.Count];
        HasBeenProcessed = new bool[paths.Count];

        for (int i = 0; i < paths.Count; ++i)
        {
            var path = paths[i];
            var pathArray = new NativeArray<Vector3>((path.Count - 1) * (int)Math.Pow(2, iterations) + 1, Allocator.Persistent);
            var pathCount = new NativeArray<int>(1, Allocator.Persistent) { [0] = path.Count };

            for (int j = 0; j < path.Count; ++j)
                pathArray[j] = path[j];

            PathArrays[i] = pathArray;
            PathCounts[i] = pathCount;

            var job = new PathSmoothingJob()
            {
                ImageSize = imageSize,
                Iterations = iterations,
                Tolerance = tolerance,
                // Shared variables
                Obstacles = NativeObstacles,
                Path = pathArray,
                PathLength = pathCount
            };

            JobHandles[i] = job.Schedule();
        }

        EditorApplication.update += UpdateJobStatus;
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

    public void RenderPaths(List<List<Vector3>> waypoint_paths)
    {
        // Render lines
        for (int i = 0; i < waypoint_paths.Count; ++i)
            RenderPath(waypoint_paths[i], i);
    }

    public void RenderPath(List<Vector3> path, int index)
    {
        GameObject pathobj = new GameObject($"Path {index}");

        pathobj.transform.parent = containers.paths.transform;
        pathobj.transform.localPosition = Vector3.zero;
        pathobj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = pathobj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;

        lr.widthMultiplier = 0.4f;
        lr.numCapVertices = 1;
        lr.material = lineMaterial;
        lr.startColor = lr.endColor = Color.black;
        lr.positionCount = path.Count;
        lr.SetPositions(path.ToArray());

        if (path.Last() == path.First())
            lr.loop = true;
    }
}
