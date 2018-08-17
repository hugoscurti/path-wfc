/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections.Generic;

using UnityEngine;

using Utilities;

public class PathOverlapModel
{
    #region Static members

    // We define only three colors
    public static Color freespace = Color.white,
        obstacle = new Color(165f / 255, 42f / 255, 42f / 255),
        path = Color.black,
        failedColor = Color.yellow;

    // Lists of offsets
    static readonly int[] DX = { -1, 0, 1, 0 };
    static readonly int[] DY = { 0, 1, 0, -1 };
    static readonly int[] opposite = { 2, 3, 0, 1 };

    // Index associated with colors
    public static int obstacle_idx = -1,
        freespace_idx = -1,
        path_idx = -1;

    #endregion

    int N;  //Edge size of pattern
    int T;  //Total number of unique patterns
    int C;  //Color counts


    Size insize;
    Size outsize;

    List<Color32> colors;    

    Mask bufferMask,
        masks;

    Pattern[] patterns;
    HashSet<int> maskPatterns;

    HashSet<int> boundaries;
    HashSet<int> obstacleBoundaries;

    HashSet<int> visited;
    Queue<int> queue;

    private bool[][] wave;
    private int[][][] compatible;

    byte[] output_idx;
    private readonly int[][][] propagator;

    FixedStack<Tuple<int, int>> indexstack;

    System.Random random;

    // Weight values
    double[] weights;
    double[] weightLogWeights;

    // Accumulated values needed to copmute logs
    int[] sumsOfOnes;
    double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    // Used in the observe function
    double[] distribution;

    public PathOverlapAttributes attributes;

    #region Contructor

    public PathOverlapModel()
    {
        Pattern.mask_idx_l1 = 0;
        Pattern.mask_idx_l2 = 1;

        colors = new List<Color32>(30);
        propagator = new int[4][][];
        maskPatterns = new HashSet<int>();
        boundaries = new HashSet<int>();
        obstacleBoundaries = new HashSet<int>();
        visited = new HashSet<int>();
        queue = new Queue<int>();
    }

    #endregion

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
        maskPatterns.Clear();
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


    private void _ExtractPattern(bool periodic, Size size, byte[] indices, bool addCount, Dictionary<long, int> counts, Dictionary<long, Pattern> dict)
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

    private void _ExtractMaskPatterns(Size size, byte[] indices, byte maskColorIdx, Dictionary<long, int> counts, Dictionary<long, Pattern> dict)
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

    

    #region Initialization

    public void Init(Texture2D inputTex, Texture2D outputTex, int N, PathOverlapAttributes attributes)
    {
        this.N = N;
        this.attributes = attributes;

        // We use outsize to get the size of the bitmap image to prevent issues when accessing bitmap images across multiple threads
        insize = new Size()
        {
            width = inputTex.width,
            height = inputTex.height
        };

        outsize = new Size() {
            width = outputTex.width,
            height = outputTex.height
        };

        colors.Clear();
        // First 2 colors represent mask colors
        colors.Add(Color.clear);
        colors.Add(Color.clear);

        byte[] indices = TileUtils.IndexColours(inputTex.GetPixels32(), colors);
        output_idx = TileUtils.IndexColours(outputTex.GetPixels32(), colors);

        FillSpecialColorIndices();

        // Instantiate masks
        Pattern.layer1 = new Mask(true, path_idx);
        Pattern.layer2 = new Mask(true);
        bufferMask = new Mask(true, path_idx, freespace_idx, obstacle_idx);
        masks = new Mask(false, Pattern.mask_idx_l1, Pattern.mask_idx_l2);

        // Set boundaries
        ConfigureBoundaries();
        ConfigureObstacleBoundaries();

        int tilecount = outsize.width * outsize.height;

        // Try to optimize array initialization
        if (wave == null || wave.Length != tilecount)
        {
            wave = new bool[tilecount][];
            compatible = new int[tilecount][][];
        }

        // We set T in this function!
        ExtractPattern(indices, this.attributes.GenerateMasksFromOutput);

        if (indexstack == null || indexstack.Size() != tilecount * T)
            indexstack = new FixedStack<Tuple<int, int>>(tilecount * T);
        else
            indexstack.Clear();

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

        distribution = new double[T];

        // Populate propagator
        for (int d = 0; d < 4; ++d)
        {
            propagator[d] = new int[T][];
            for (int t = 0; t < T; ++t)
                patterns[t].Overlap(patterns, DX[d], DY[d], out propagator[d][t]);
        }
    }

    /// <summary>
    /// Clear stuff and prepare object for new execution
    /// </summary>
    public void Reset(int seed)
    {
        Clear();
        FixInitialValues();
        random = new System.Random(seed);
    }

    /// <summary>
    /// Reset wave, compatible array and index stack
    /// </summary>
    public void Clear()
    {
        indexstack.Clear();

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

        for (int y = 0; y < outsize.height; ++y)
            for (int x = 0; x < outsize.width; ++x)
            {
                if (OnBoundary(x, y)) continue;

                i = y * outsize.width + x;
                w = wave[i];

                for (int t = 0; t < T; ++t)
                    if (w[t] && w[t] != PatternFits(x, y, patterns[t]))
                        Ban(i, t);

                // Filter out patterns that are enclosed in a masked pattern
                foreach (int t in maskPatterns)
                    if (w[t]) FilterPatternsThatFitMask(t, i);
            }
    }

    #endregion

    /// <summary>
    /// Verify wether the pattern indexed at t can fit into
    /// the output at index i
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
    public void ConfigureBoundaries()
    {
        // Breadth or depth first search on all borders
        boundaries.Clear();
        visited.Clear();
        queue.Clear();

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
    }

    public void ConfigureObstacleBoundaries()
    {
        obstacleBoundaries.Clear();
        queue.Clear();

        int i;

        for (int x = 1; x < outsize.width - 1; ++x)
            for (int y = 1; y < outsize.height - 1; ++y)
            {
                i = y * outsize.width + x;
                if (visited.Contains(i)) continue;

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
                //failed = true;
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
    public Size GetOutputRect()
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
    public void Print(Texture2D output)
    {
        bool[] w;
        int i;

        // Use this to store result
        Color32[] raw_output = output.GetPixels32();

        int x, y;

        for (y = 0; y < outsize.height; ++y)
            for (x = 0; x < outsize.width; ++x)
            {
                int contributors = 0, alpha_contrib = 0, r = 0, g = 0, b = 0, a = 0;

                for (int dy = 0; dy < N; ++dy)
                    for (int dx = 0; dx < N; ++dx)
                    {
                        int sx = x - dx;
                        if (sx < 0) sx += outsize.width;

                        int sy = y - dy;
                        if (sy < 0) sy += outsize.height;

                        if (OnBoundary(sx, sy)) continue;

                        i = sy * outsize.width + sx;
                        w = wave[i];
                        for (int t = 0; t < T; ++t)
                        {
                            if (w[t])
                            {
                                Color32 c = colors[patterns[t].Get(dx, dy)];

                                alpha_contrib++;
                                a += c.a;

                                if (c.a != 0)
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
                    if (contributors == 0)
                        // Only transparent contributors
                        raw_output[y * outsize.width + x] = new Color32(0, 0, 0, 0);
                    else
                        raw_output[y * outsize.width + x] = new Color32((byte)(r / contributors), (byte)(g / contributors), (byte)(b / contributors), (byte)(a / alpha_contrib));
                } else
                {
                    raw_output[y * outsize.width + x] = failedColor;
                }
            }

        output.SetPixels32(raw_output);
        output.Apply();
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
