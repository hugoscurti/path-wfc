using System;
using System.Collections.Generic;

using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

using Utilities;

public class PathOverlapModel
{
    int N;  //Edge size of pattern
    int T;  //Total number of unique patterns
    int C;  //Color counts

    Color failedColor = Color.yellow;
    bool failed;
    Vector2Int failedAt;

    // TODO: Use something more abstract than tilemap, if we'd want to port this to other types of maps
    Tilemap input, output;

    RectInt outsize;
    RectInt insize;

    List<Color> colors;

    // We define only three colors
    public static Color freespace = Color.white,
        obstacle = new Color(165f / 255, 42f / 255, 42f / 255),
        path = Color.black;

    // Index associated with colors
    public static int obstacle_idx = -1,
        freespace_idx = -1,
        path_idx = -1;

    Mask bufferMask,
        masks;

    // Lists of offsets
    static readonly int[] DX = { -1, 0, 1, 0 };
    static readonly int[] DY = { 0, 1, 0, -1 };
    static readonly int[] opposite = { 2, 3, 0, 1 };

    Pattern[] patterns;
    HashSet<int> maskPatterns;

    HashSet<int> boundaries;
    HashSet<int> obstacleBoundaries;

    bool[][] wave;
    int[][][] compatible;

    byte[] output_idx;

    int[][][] propagator;

    FixedStack<Tuple<int, int>> indexstack;

    System.Random random;

    // Weight values
    double[] weights;
    double[] weightLogWeights;

    // Accumulated values needed to copmute logs
    int[] sumsOfOnes;
    double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    public PathOverlapAttributes attributes;

    #region Utility Functions


    /// <summary>
    /// Find indices for the color corresponding to obstacle and freespace
    /// </summary>
    private void FillSpecialColorIndices()
    {
        C = colors.Count;
        obstacle_idx = colors.IndexOf(obstacle);
        freespace_idx = colors.IndexOf(freespace);
        path_idx = colors.IndexOf(path);
    }

    /// <summary>
    /// Convert color into indices.
    /// Since we know exactly the expected color, we can hard-code the color indices
    /// and look for them.
    /// </summary>
    private byte[] IndexColours(Tilemap source, RectInt size)
    {
        Color c;
        int idx;

        byte[] indices = new byte[size.width * size.height];
        Vector3Int vi = Vector3Int.zero;

        for (vi.y = 0; vi.y < size.height; ++vi.y)
            for (vi.x = 0; vi.x < size.width; ++vi.x)
            {
                c = source.GetColor(vi);
                idx = colors.IndexOf(c);
                if (idx == -1)
                {
                    idx = colors.Count;
                    colors.Add(c);
                }

                indices[vi.y * size.width + vi.x] = (byte)idx;
            }

        return indices;
    }


    private bool OnBoundary(int x, int y)
    {
        return !attributes.PeriodicOutput &&
            (x + N > outsize.width || y + N > outsize.height
            || x < 0 || y < 0);
    }

    private int WrapAround(int i2, int length)
    {
        if (i2 < 0) return i2 + length;
        else if (i2 >= length) return i2 - length;
        else return i2;
    }


    private void AddPattern(Pattern pattern, Dictionary<long, int> counts, Dictionary<long, Pattern> dict, bool addCount)
    {
        long index = pattern.GetIndex(C);

        if (counts.ContainsKey(index))
        {
            if (addCount)
                counts[index]++;
        }
        else
        {
            counts.Add(index, 1);
            dict.Add(index, pattern);
        }
    }

    private void ExtractPattern(byte[] indices, bool fromOutput)
    {
        // Generate all patterns
        // Store them using a unique identifier
        Dictionary<long, int> patternCounts = new Dictionary<long, int>();
        Dictionary<long, Pattern> patternDict = new Dictionary<long, Pattern>();

        // Manually insert the freespace pattern
        AddPattern(new Pattern(N, (byte)freespace_idx), patternCounts, patternDict, true);

        _ExtractPattern(attributes.PeriodicInput, insize, indices, true, patternCounts, patternDict);

        if (fromOutput)
            // If pattern exists, we don't add the count to it
            _ExtractMaskPatterns(outsize, output_idx, (byte)freespace_idx, patternCounts, patternDict);

        // Set propagators and other things
        T = patternCounts.Count;
        Debug.Log($"Number of patterns: {T}");
        patterns = new Pattern[T];
        maskPatterns = new HashSet<int>();
        weights = new double[T];

        int idx = 0;
        foreach (long key in patternCounts.Keys)
        {
            patterns[idx] = patternDict[key];
            weights[idx] = attributes.UseRandomWeights ? 1 : patternCounts[key];

            // Keep track of patterns with mask colors
            if (patterns[idx].ContainsMasks())
                maskPatterns.Add(idx);

            idx++;
        }
    }


    private void _ExtractPattern(bool periodic, RectInt size, byte[] indices, bool addCount, Dictionary<long, int> counts, Dictionary<long, Pattern> dict)
    {
        Pattern p;
        for (int y = 0; y < (periodic ? size.height : size.height - N + 1); ++y)
            for (int x = 0; x < (periodic ? size.width : size.width - N + 1); ++x)
            {
                p = new Pattern(N, x, y, indices, size);

                AddPattern(p, counts, dict, addCount);

                if (attributes.AddRotationsAndReflexions)
                {
                    // Add rotations and reflexions
                    AddPattern(p.Reflect(), counts, dict, addCount);
                    Pattern p1 = p.Rotate();
                    AddPattern(p1, counts, dict, addCount);

                    AddPattern(p1.Reflect(), counts, dict, addCount);
                    p1 = p1.Rotate();
                    AddPattern(p1, counts, dict, addCount);

                    AddPattern(p1.Reflect(), counts, dict, addCount);
                    p1 = p1.Rotate();
                    AddPattern(p1, counts, dict, addCount);

                    AddPattern(p1.Reflect(), counts, dict, addCount);
                }
            }
    }

    private void _ExtractMaskPatterns(RectInt size, byte[] indices, byte maskColorIdx, Dictionary<long, int> counts, Dictionary<long, Pattern> dict)
    {
        // Extract patterns
        Pattern p;
        for (int y = 0; y < size.height - N + 1; ++y)
            for (int x = 0; x < size.width - N + 1; ++x)
            {
                p = new Pattern(N, x, y, indices, size, maskColorIdx);

                // Disregard patterns that only has maskcolor
                if (p.ContainsOnlyMasks())
                    continue;

                AddPattern(p, counts, dict, false);

                // No rotation/reflexions on output patterns
            }
    }

    #endregion

    #region Contructor

    public PathOverlapModel(Tilemap input, Tilemap output, int N, PathOverlapAttributes attributes)
    {
        this.N = N;
        this.input = input;
        this.output = output;
        this.attributes = attributes;

        // We use outsize to get the size of the bitmap image to prevent issues when accessing bitmap images across multiple threads
        this.insize = this.input.GetBounds();
        this.outsize = this.output.GetBounds();

        colors = new List<Color>() {
            // Add mask index as first 2 colors (both layer masks)
            Color.clear,
            Color.clear
        };

        Pattern.mask_idx_l1 = 0;
        Pattern.mask_idx_l2 = 1;

        byte[] indices = IndexColours(input, insize);
        output_idx = IndexColours(output, outsize);

        FillSpecialColorIndices();

        // Instantiate masks
        Pattern.layer1 = new Mask(true, path_idx);
        Pattern.layer2 = new Mask(true);
        bufferMask = new Mask(true, path_idx, freespace_idx, obstacle_idx);
        masks = new Mask(false, Pattern.mask_idx_l1, Pattern.mask_idx_l2);

        // Set boundaries
        HashSet<int> visited = ConfigureBoundaries();
        ConfigureObstacleBoundaries(visited);

        int tilecount = outsize.width * outsize.height;

        wave = new bool[tilecount][];
        compatible = new int[tilecount][][];

        // We set T in this function!
        ExtractPattern(indices, this.attributes.GenerateMasksFromOutput);

        indexstack = new FixedStack<Tuple<int, int>>(tilecount * T);

        // Initialize wave and compatible arrays
        for (int i = 0; i < wave.Length; ++i)
        {
            wave[i] = new bool[T];
            compatible[i] = new int[T][];
            for (int t = 0; t < T; ++t) compatible[i][t] = new int[4];
        }

        // Initialize fixed arrays
        weightLogWeights = new double[T];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        for (int t = 0; t < T; t++)
        {
            weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
            sumOfWeights += weights[t];
            sumOfWeightLogWeights += weightLogWeights[t];
        }

        startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        // Intialize sum arrays
        sumsOfOnes = new int[tilecount];
        sumsOfWeights = new double[tilecount];
        sumsOfWeightLogWeights = new double[tilecount];
        entropies = new double[tilecount];


        // Populate propagator
        propagator = new int[4][][];
        for (int d = 0; d < 4; ++d)
        {
            propagator[d] = new int[T][];
            for (int t = 0; t < T; ++t)
                propagator[d][t] = patterns[t].Overlap(patterns, DX[d], DY[d]).ToArray();
        }
    }

    #endregion

    /// <summary>
    /// Verify wether the pattern indexed at t can fit into
    /// the output at index i
    /// 
    /// TODO: would there be some way to loosen the comparison with obstacles? i.e. the general form of the pattern matches but 
    /// </summary>
    public bool PatternFits(int x, int y, Pattern p)
    {
        byte patt_col, out_col;

        // 2. Match the pattern with the output
        for (int dx = 0; dx < N; ++dx)
            for (int dy = 0; dy < N; ++dy)
            {
                int sx = (x + dx) % outsize.width,
                    sy = (y + dy) % outsize.height;

                patt_col = p.Get(dx, dy);
                out_col = output_idx[sy * outsize.width + sx];

                if (out_col == Pattern.mask_idx_l1)
                {
                    if (!Pattern.layer1.Agrees(patt_col)) return false;
                    else continue;
                } else if (out_col == Pattern.mask_idx_l2)
                {
                    if (!Pattern.layer2.Agrees(patt_col)) return false;
                }

                // See if pattern matches the area represented by index i.
                if ((patt_col == obstacle_idx) != (out_col == obstacle_idx))
                    return false;
            }

        // Check for borders
        if (attributes.forbidBufferSpaceOnBoundaries && IsBoundaryBuffer(x, y, p)) return false;

        if (attributes.enforceBufferSpaceOnObstacles && IsNotObstacleBoundaryBuffer(x, y, p)) return false;



        return true;
    }

    public void FindBoundary(Queue<int> queue, HashSet<int> visited, HashSet<int> result)
    {
        int i;
        int x, y, tempx, tempy;

        // Dequeue stuff and determine if boundary or not
        while (queue.Count > 0)
        {
            i = queue.Dequeue();

            if (visited.Contains(i)) continue;
            else visited.Add(i);

            // If i is not an obstacle, then it's a boundary
            if (output_idx[i] != obstacle_idx)
                result.Add(i);
            else
            {
                x = i % outsize.width;
                y = i / outsize.width;
                // Add adjacent tiles to queue

                // Generate offsets
                for (tempx = x - 1; tempx <= x + 1; ++tempx)
                    for (tempy = y - 1; tempy <= y + 1; ++tempy)
                    {
                        if (tempx == 0 && tempy == 0) continue;
                        if (tempx < 0 || tempy < 0 || tempx >= outsize.width || tempy >= outsize.height) continue;

                        queue.Enqueue(tempy * outsize.width + tempx);
                    }
            }
        }
    }

    /// <summary>
    /// Set actual boundaries (in the case where obstacles are found on boundaries)
    /// And filter obstacle boundaries (i.e. no white tiles?)
    /// 
    /// Returns the set of visited tiles
    /// </summary>
    public HashSet<int> ConfigureBoundaries()
    {
        // Breadth or depth first search on all borders
        boundaries = new HashSet<int>();
        HashSet<int> visited = new HashSet<int>();
        Queue<int> queue = new Queue<int>();

        int x, y;

        // 1. fill queue from boundaries

        // Horizontal boundaries
        for ( x = 0; x < outsize.width; ++x)
        {
            queue.Enqueue(x);
            queue.Enqueue((outsize.height - 1) * outsize.width + x);
        }

        // Vertical boundaries
        for ( y = 1; y < outsize.height - 1; ++y)
        {
            queue.Enqueue(y * outsize.width);
            queue.Enqueue(y * outsize.width + (outsize.width - 1));
        }

        FindBoundary(queue, visited, boundaries);

        return visited;
    }

    public void ConfigureObstacleBoundaries(HashSet<int> visitedBoundaries)
    {
        obstacleBoundaries = new HashSet<int>();

        HashSet<int> visited = new HashSet<int>();
        Queue<int> queue = new Queue<int>();
        int i;

        for (int x = 1; x < outsize.width - 1; ++x)
            for (int y = 1; y < outsize.height - 1; ++y)
            {
                i = y * outsize.width + x;
                if (visitedBoundaries.Contains(i)) continue;

                if (output_idx[i] == obstacle_idx)
                    queue.Enqueue(i);
            }

        // Find boundaries
        FindBoundary(queue, visited, obstacleBoundaries);
    }


    public bool IsBoundaryBuffer(int x, int y, Pattern p)
    {
        int i, patt;
        for( int dx = 0; dx < N; ++dx)
            for (int dy = 0; dy < N; ++dy)
            {
                i = (y + dy) * outsize.width + (x + dx);
                if (boundaries.Contains(i))
                {
                    patt = p.Get(dx, dy);

                    // It's on boundary
                    if (!masks.Agrees(patt) && bufferMask.Agrees(patt))
                        return true;
                }
            }

        // None of them are boundaries
        return false;
    }

    public bool IsNotObstacleBoundaryBuffer(int x, int y, Pattern p)
    {
        int i, patt;
        for (int dx = 0; dx < N; ++dx)
            for (int dy = 0; dy < N; ++dy)
            {
                i = (y + dy) * outsize.width + (x + dx);
                if (obstacleBoundaries.Contains(i))
                {
                    patt = p.Get(dx, dy);

                    // It's on boundary
                    if (!masks.Agrees(patt) && !bufferMask.Agrees(patt))
                        return true;
                }
            }

        // None of them are boundaries
        return false;
    }


    /// <summary>
    /// Reset wave and changes
    /// </summary>
    public void Clear()
    {
        // 1. Clear everything
        for (int i = 0; i < wave.Length; i++)
        {
            for (int t = 0; t < T; t++)
            {
                wave[i][t] = true;
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
            }

            sumsOfOnes[i] = weights.Length;
            sumsOfWeights[i] = sumOfWeights;
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
            entropies[i] = startingEntropy;
        }
    }

    public void FixInitialValues()
    {
        int i;
        bool[] w;

        // 2. Fix initial values
        for( int y = 0; y < outsize.height; ++y)
            for (int x = 0; x < outsize.width; ++x)
            {
                if (OnBoundary(x, y)) continue;

                i = y * outsize.width + x;
                w = wave[i];

                for (int t = 0; t < T; ++t)
                    if (w[t] && w[t] != PatternFits(x, y, patterns[t])) Ban(i, t);

                // Filter out patterns that are enclosed in a masked pattern
                foreach (int t in maskPatterns)
                    if (w[t]) FilterPatternsThatFitMask(t, i);
            }
    }

    private void FilterPatternsThatFitMask(int mask_pattern, int i)
    {
        Pattern mp = patterns[mask_pattern];
        Pattern p;
        bool fits;
        bool[] w = wave[i];
        for ( int t = 0; t < T; ++t)
        {
            if (!w[t]) continue;    // Already false, doesn't need to try to filter it up
            if (t == mask_pattern) continue;

            p = patterns[t];
            fits = true;
            for ( int x = 0; x < N; ++x)
                for (int y = 0; y < N; ++y)
                {
                    if (mp.Get(x, y) == mask_pattern && p.Get(x, y) == path_idx)
                    {
                        fits = false;
                        break;
                    }
                }

            // If pattern fits the mask then we ban it
            if (fits) Ban(i, t);
        }
    }

    /// <summary>
    /// Clear stuff and prepare object for new execution
    /// </summary>
    public void Init(int seed)
    {
        indexstack.Clear();

        Clear();
        FixInitialValues();
        random = new System.Random(seed);
    }


    private void UpdateLogsAndEntropies(int i, int t)
    {
        double sum = sumsOfWeights[i];
        entropies[i] += sumsOfWeightLogWeights[i] / sum - Math.Log(sum);

        sumsOfOnes[i] -= 1;
        sumsOfWeights[i] -= weights[t];
        sumsOfWeightLogWeights[i] -= weightLogWeights[t];

        sum = sumsOfWeights[i];
        entropies[i] -= sumsOfWeightLogWeights[i] / sum - Math.Log(sum);
    }

    /// <summary>
    /// Ban elements from the observed states
    /// </summary>
    private void Ban(int i, int t)
    {
        wave[i][t] = false;

        // Set the number of compatible patterns to 0
        int[] comp = compatible[i][t];
        for (int d = 0; d < 4; ++d) comp[d] = 0;

        indexstack.Push(new Tuple<int, int>(i, t));

        UpdateLogsAndEntropies(i, t);
    }

    public void FixWave(Vector2Int pos, int patternIdx)
    {
        int i = pos.y * outsize.width + pos.x;

        bool[] w = wave[i];

        for (int t = 0; t < T; ++t)
            if (w[t] != (t == patternIdx)) Ban(i, t);

        Propagate();
    }


    /// <summary>
    /// Calculate entropy for each pattern, for each position in output
    /// </summary>
    public bool? Observe()
    {
        double minEnt = 1e+3;
        int indexmin = -1;

        for (int i = 0; i < wave.Length; ++i)
        {
            if (OnBoundary(i % outsize.width, i / outsize.width)) continue;

            int amount = sumsOfOnes[i];
            if (amount == 0)
            {
                Debug.Log($"Failed at { i % outsize.width },{i / outsize.width}");
                //failedAt = new Vector2Int(i % outsize.width, i / outsize.width);
                failed = true;
                return false;
            }

            double entropy = entropies[i];
            if (amount > 1 && entropy <= minEnt)
            {
                entropy += 1e-6 * random.NextDouble(); // add noise to entropy
                if (entropy < minEnt)
                {
                    minEnt = entropy;
                    indexmin = i;
                }
            }
        }

        // Returning true means the algorithm converged
        if (indexmin == -1)
            return true;

        double[] distribution = new double[T];
        bool[] w = wave[indexmin];

        // Fill in weights
        for (int t = 0; t < T; t++)
            distribution[t] = w[t] ? weights[t] : 0;

        // Choose one pattern indexed at r
        int r = distribution.Random(random.NextDouble());

        // Ban all patterns except r
        for (int t = 0; t < T; t++)
            if (w[t] != (t == r)) Ban(indexmin, t);

        // Algorithm is not finished yet
        return null;
    }

    public void Propagate()
    {
        while (!indexstack.IsEmpty())
        {
            Tuple<int, int> e1 = indexstack.Pop();

            _Propagate(e1);
        }
    }

    /// <summary>
    /// Break down of the Propagate function that only process one position at a time
    /// </summary>
    /// <returns> False if nothing happened, true if we executed something</returns>
    public bool PropagateOne()
    {
        if (indexstack.IsEmpty())
            return false;

        Tuple<int, int> e1 = indexstack.Pop();

        _Propagate(e1);

        return true;
    }

        
    private void _Propagate(Tuple<int, int> e1)
    {
        int i1 = e1.Item1;
        int x1 = i1 % outsize.width,
            y1 = i1 / outsize.width;

        // Iterate over all overlap combinations
        for (int d = 0; d < 4; ++d)
        {
            int x2 = x1 + DX[d], y2 = y1 + DY[d];
            // If on boundary
            if (OnBoundary(x2, y2)) continue;

            // Wrap around if necessary
            x2 = WrapAround(x2, outsize.width);
            y2 = WrapAround(y2, outsize.height);
            int i2 = y2 * outsize.width + x2;

            int[] p = propagator[d][e1.Item2];
            int[][] compat = compatible[i2];

            for (int l = 0; l < p.Length; ++l)
            {
                int t2 = p[l];
                int[] comp = compat[t2];

                if (--comp[d] == 0) Ban(i2, t2);
            }
        }
    }

    /// <summary>
    /// Returns the list of indices that represent obstacle
    /// </summary>
    public HashSet<int> GetObstacles()
    {
        HashSet<int> obstacles = new HashSet<int>();

        for (int i = 0; i < output_idx.Length; ++i)
            if (output_idx[i] == obstacle_idx)
                obstacles.Add(i);

        return obstacles;
    }

    /// <summary>
    /// Returns the size of the output map
    /// </summary>
    public RectInt GetOutputRect()
    {
        return outsize;
    }


    public bool[] IsPath()
    {
        int i, si;
        bool[] w;

        // Store unique patterns for each input
        bool[] arePaths = new bool[outsize.width * outsize.height];

        for (int y = 0; y < outsize.height; ++y)
            for (int x = 0; x < outsize.width; ++x)
            {
                i = y * outsize.width + x;

                for (int dy = 0; dy < N; ++dy)
                    for (int dx = 0; dx < N; ++dx)
                    {
                        int sx = x - dx;
                        if (sx < 0) sx += outsize.width;

                        int sy = y - dy;
                        if (sy < 0) sy += outsize.height;

                        if (OnBoundary(sx, sy)) continue;

                        si = sy * outsize.width + sx;

                        // TODO: Do we assume that we have a good result everywhere?   
                        w = wave[si];
                        for (int t = 0; t < T; ++t)
                        {
                            if (w[t])
                            {
                                int ind = patterns[t].Get(dx, dy);
                                if (masks.Agrees(ind))
                                    // Don't care about mask for now
                                    continue;

                                bool isPath = ind == path_idx;

                                if (arePaths[i] && !isPath)
                                {
                                    // Ignore path
                                    arePaths[i] = false;
                                    Debug.LogError($"Color indices are different! x={x}, y={y}");
                                }
                                else
                                    arePaths[i] = isPath;
                            }
                        }
                    }
            }

        return arePaths;
    }


    /// <summary>
    /// Print in the array of tiles 
    /// </summary>
    /// <param name="tiles"></param>
    public void Print()
    {
        bool[] w;
        int i;

        // Use this to store result
        Vector3Int p = new Vector3Int();

        for (p.y = 0; p.y < outsize.height; ++p.y)
            for (p.x = 0; p.x < outsize.width; ++p.x)
            {
                float contributors = 0, alpha_contrib = 0, r = 0, g = 0, b = 0, a = 0;

                for (int dy = 0; dy < N; ++dy)
                    for (int dx = 0; dx < N; ++dx)
                    {
                        int sx = p.x - dx;
                        if (sx < 0) sx += outsize.width;

                        int sy = p.y - dy;
                        if (sy < 0) sy += outsize.height;

                        if (OnBoundary(sx, sy)) continue;

                        i = sy * outsize.width + sx;
                        w = wave[i];
                        for (int t = 0; t < T; ++t)
                        {
                            if (w[t])
                            {
                                Color c = colors[patterns[t].Get(dx, dy)];

                                alpha_contrib++;
                                a += c.a;

                                if (c != Color.clear)
                                {
                                    contributors++;
                                    r += c.r;
                                    g += c.g;
                                    b += c.b;
                                }
                            }
                        }
                    }

                if (alpha_contrib != 0)
                {
                    Color newCol = new Color(r / contributors, g / contributors, b / contributors, a / alpha_contrib);

                    // An attempt at changing only the relevant tiles (i.e. change it only if the color is different)
                    if (output.GetColor(p) != newCol)
                    {
                        output.SetColor(p, newCol);
                    }
                } else
                {
                    output.SetColor(p, failedColor);
                }
            }
    }

    /// <summary>
    /// Return the list of pattern that are possible for the selected position
    /// </summary>
    public Dictionary<int, Texture2D> GetPatternsForWave(Vector2Int p)
    {
        Dictionary<int, Texture2D> res = new Dictionary<int, Texture2D>();

        bool[] w = wave[p.y * outsize.width + p.x];
        for (int t = 0; t < T; ++t)
        {
            if (w[t])
                res.Add(t, patterns[t].Print(colors));
        }

        return res;
    }


}
