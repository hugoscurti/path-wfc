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
    int overlap_N; //Maximum overlap size

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

    // Mask index represent a tile that can be fit by anything
    int mask_idx = -1;


    Pattern[] patterns;
    HashSet<int> maskPatterns;

    bool[,][] wave;
    double[] stationary;
    byte[,] output_idx;

    int[,][][] propagator;

    bool[,] changes;
    Stack<Vector2Int> indexstack;

    System.Random random;

    double[] logProb; //Log prob for each pattern
    double logT;

    PathOverlapAttributes attributes;

    #region Utility Functions

    /// <summary>
    /// Return the index of Color c from the colors array
    /// </summary>
    private byte GetColorIndex(Color c)
    {
        var idx = colors.IndexOf(c);

        if (idx == -1)
            throw new NotSupportedException("Color not found");

        return (byte)idx;
    }


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
    private byte[,] IndexColours(Tilemap source, RectInt size)
    {
        Color c;
        int idx;

        byte[,] indices = new byte[size.width, size.height];

        for (int y = 0; y < size.height; ++y)
            for (int x = 0; x < size.width; ++x)
            {
                c = source.GetColor(new Vector3Int(x, y, 0));
                idx = colors.IndexOf(c);
                if (idx == -1)
                {
                    idx = colors.Count;
                    colors.Add(c);
                }

                indices[x, y] = (byte)idx;
            }

        return indices;
    }

    /// <summary>
    /// Put an index on the stack
    /// </summary>
    private void Change(Vector2Int p)
    {
        if (changes[p.x, p.y]) return;

        indexstack.Push(p);
        changes[p.x, p.y] = true;
    }


    private void FillLogs()
    {
        logT = Math.Log(T);
        logProb = new double[T];
        for (int t = 0; t < T; t++)
            logProb[t] = Math.Log(stationary[t]);
    }

    private bool OnBoundary(int x, int y)
    {
        return !attributes.PeriodicOutput &&
            (x + N > outsize.width || y + N > outsize.height);
    }

    private int GetSecondIndex(int i1, int di, int length)
    {
        int i2 = i1 + di;
        if (i2 < 0) i2 += length;
        else if (i2 >= length) i2 -= length;
        return i2;
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

    private void ExtractPattern(byte[,] indices, bool fromOutput)
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
            _ExtractPattern(attributes.PeriodicOutput, outsize, output_idx, false, patternCounts, patternDict, (byte?)freespace_idx);

        // Set propagators and other things
        T = patternCounts.Count;
        patterns = new Pattern[T];
        maskPatterns = new HashSet<int>();
        stationary = new double[T];
        propagator = new int[overlap_N, overlap_N][][];

        int idx = 0;
        foreach (long key in patternCounts.Keys)
        {
            patterns[idx] = patternDict[key];
            stationary[idx] = patternCounts[key];

            // Keep track of patterns with mask colors
            if (patterns[idx].ContainsColor((byte)mask_idx))
                maskPatterns.Add(idx);

            idx++;
        }
    }


    private void _ExtractPattern(bool periodic, RectInt size, byte[,] indices, bool addCount, Dictionary<long, int> counts, Dictionary<long, Pattern> dict, byte? maskColorIdx = null)
    {
        Pattern p;
        for (int y = 0; y < (periodic ? size.height : size.height - N + 1); ++y)
            for (int x = 0; x < (periodic ? size.width : size.width - N + 1); ++x)
            {
                p = new Pattern(N, x, y, indices, maskColorIdx);

                // Disregard patterns that only has maskcolor
                if (maskColorIdx.HasValue && p.ContainsOnly((byte)mask_idx))
                    continue;

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

    #endregion

    #region Contructor

    public PathOverlapModel(Tilemap input, Tilemap output, int N, PathOverlapAttributes attributes)
    {
        this.N = N;
        this.overlap_N = 2 * N - 1;
        this.input = input;
        this.output = output;
        this.attributes = attributes;

        // We use outsize to get the size of the bitmap image to prevent issues when accessing bitmap images across multiple threads
        this.insize = this.input.GetBounds();
        this.outsize = this.output.GetBounds();

        colors = new List<Color>() {
            // Add mask index as first color
            Color.clear
        };
        this.mask_idx = 0;  // TODO: Store it only at one place
        Pattern.mask_idx = 0;

        byte[,] indices = IndexColours(input, insize);
        output_idx = IndexColours(output, outsize);

        FillSpecialColorIndices();

        wave = new bool[outsize.width, outsize.height][];
        changes = new bool[outsize.width, outsize.height];
        indexstack = new Stack<Vector2Int>();

        ExtractPattern(indices, this.attributes.GenerateMasksFromOutput);

        // Initialize wave array
        wave.ForEach((x, y) => wave[x, y] = new bool[T]);

        // Populate propagator
        for (int x = 0; x < overlap_N; ++x)
            for (int y = 0; y < overlap_N; ++y)
            {
                propagator[x, y] = new int[T][];
                int dx = x - N + 1,
                    dy = y - N + 1;

                for (int t = 0; t < T; ++t)
                    propagator[x, y][t] = patterns[t].Overlap(patterns, dx, dy).ToArray();
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
                patt_col = p.Get(dx, dy);
                out_col = output_idx[(x + dx) % outsize.width, (y + dy) % outsize.height];

                if (out_col == mask_idx)
                {
                    if (patt_col == path_idx) return false;
                    else continue;
                }

                // See if pattern matches the area represented by index i.
                if ((patt_col == obstacle_idx) != (out_col == obstacle_idx))
                    return false;
            }
        return true;
    }

    /// <summary>
    /// Reset wave and changes
    /// </summary>
    public void Clear()
    {
        wave.ForEach((w, x, y) =>
        {
            if (OnBoundary(x, y)) return;

            for (int t = 0; t < T; ++t)
                w[t] = PatternFits(x, y, patterns[t]);

            // Filter out patterns that are enclosed in a masked pattern
            foreach (int t in maskPatterns)
            {
                if (w[t])
                    FilterPatternsThatFitMask(t, w);
            }

            changes[x, y] = false;
        });
    }

    private void FilterPatternsThatFitMask(int mask_pattern, bool[] w)
    {
        Pattern mp = patterns[mask_pattern];
        Pattern p;
        bool fits;
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
            if (fits)
                w[t] = false;
        }
    }

    /// <summary>
    /// Clear stuff and prepare object for new execution
    /// </summary>
    public void Init(int seed)
    {
        indexstack.Clear();

        FillLogs();
        Clear();
        random = new System.Random(seed);
    }


    private double CalculateEntropy(bool[] w, double amount, double sum)
    {
        if (amount == 1) return 0;
        else if (amount == T) return logT;
        else
        {
            double mainSum = 0;
            for (int t = 0; t < T; ++t)
                if (w[t]) mainSum += stationary[t] * logProb[t];

            return Math.Log(sum) - mainSum / sum;
        }
    }

    public void FixWave(Vector2Int pos, int patternIdx)
    {
        bool[] w = wave[pos.x, pos.y];

        for (int t = 0; t < T; ++t)
            w[t] = t == patternIdx;

        Change(pos);
        Propagate();
    }

    /// <summary>
    /// Gets all the tiles that have fixed pattern (i.e. one possible pattern) and propagates them)
    /// </summary>
    public void PropagateFixedWaves(bool propagate)
    {
        wave.ForEach((w, x, y) => {
            if (OnBoundary(x, y)) return;

            if (w.Count(t => t) == 1)
                Change(new Vector2Int(x, y));
        });

        if (propagate)
            Propagate();
    }


    /// <summary>
    /// Propagate the tiles that have masked color in them (since they are more constrained)
    /// </summary>
    /// <param name="propagate"></param>
    public void PropagateMasks(bool propagate)
    {
        wave.ForEach((w, x, y) => {
            if (OnBoundary(x, y)) return;

            foreach (int i in maskPatterns)
            {
                if (w[i])
                {
                    Change(new Vector2Int(x, y));
                    break;
                }
            }
        });

        if (propagate)
            Propagate();
    }

    /// <summary>
    /// Calculate entropy for each pattern, for each position in output
    /// </summary>
    public bool? Observe()
    {
        double minEnt = 1e+3;
        Vector2Int indexminEnt = new Vector2Int(-1, -1);

        double minCount = Double.PositiveInfinity;
        Vector2Int indexminCount = new Vector2Int(-1, -1);

        bool[] w;


        for (int x = 0; x <wave.GetLength(0); ++x)
            for (int y = 0; y <wave.GetLength(1); ++y)
            {
                if (OnBoundary(x, y)) continue;

                w = wave[x, y];
                double amount = 0;
                double sum = 0;

                for (int t = 0; t < T; ++t)
                {
                    if (w[t])
                    {
                        amount += 1;
                        sum += stationary[t];
                    }
                }

                // Cannot divide by zero, and we divide by the sum when finding the entropy
                if (sum == 0) return false;

                // Calculate entropy
                double noise = 1e-6 * random.NextDouble();
                double entropy = CalculateEntropy(w, amount, sum);
                
                if (amount > 1 && amount + noise < minCount)
                {
                    minCount = amount + noise;
                    indexminCount.x = x;
                    indexminCount.y = y;
                }

                // Store as min if smaller than min
                // For equal values, we use a small random value to decide which one to use
                if (entropy > 0 && entropy + noise < minEnt)
                {
                    minEnt = entropy + noise;
                    indexminEnt.x = x;
                    indexminEnt.y = y;
                }
            }

        // Returning true means the algorithm converged
        if (indexminEnt.x == -1)
            return true;

        double[] distribution = new double[T];
        bool[] w1 = wave[indexminEnt.x, indexminEnt.y];

        for (int t = 0; t < T; t++)
            distribution[t] = w1[t] ? stationary[t] : 0;

        // Choose one pattern 
        int r = distribution.Random(random.NextDouble());

        for (int t = 0; t < T; t++)
            // Set one pattern r in T to be observed. Set others to false
            w1[t] = t == r;

        Change(indexminEnt);

        // Algorithm is not finished yet
        return null;
    }

    public void Propagate()
    {
        while (indexstack.Count > 0)
        {
            Vector2Int i1 = indexstack.Pop();
            changes[i1.x, i1.y] = false;

            _Propagate(i1);
        }
    }

    /// <summary>
    /// Break down of the Propagate function that only process one position at a time
    /// </summary>
    /// <returns> False if nothing happened, true if we executed something</returns>
    public bool PropagateOne()
    {
        if (indexstack.Count == 0)
            return false;

        Vector2Int i1 = indexstack.Pop();
        changes[i1.x, i1.y] = false;

        _Propagate(i1);

        return true;
    }

        
    private void _Propagate(Vector2Int i1)
    {
        bool[] w1 = wave[i1.x, i1.y];

        if (w1.All(w => !w)) {
            // For debug purposes
        }

        // Iterate over all overlap combinations
        for (int dx = -N + 1; dx < N; ++dx)
            for (int dy = -N + 1; dy < N; ++dy)
            {
                int x2 = GetSecondIndex(i1.x, dx, outsize.width),
                    y2 = GetSecondIndex(i1.y, dy, outsize.height);

                // If on boundary
                if (OnBoundary(x2, y2)) continue;

                bool[] w2 = wave[x2, y2];
                int[][] prop = propagator[N - 1 - dx, N - 1 - dy];

                for (int t2 = 0; t2 < T; ++t2)
                {
                    if (!w2[t2]) continue;

                    int[] p = prop[t2];

                    // If no overlap, then we set it to false and put in the stack
                    if (p.Length > 0 && p.All(i => !w1[i]))
                    {
                        // If we haven't found an overlap, set w2 to false
                        w2[t2] = false;
                        Change(new Vector2Int(x2, y2));
                    }

                    if (w2.All(w => !w))
                        // Stops propagation when it fails
                        return;
                }
            }
    }

    public void Print()
    {
        Tile blanktile = Resources.Load<Tile>("Tiles/White");

        bool[] w;

        wave.ForEach((x, y) => {
            float contributors = 0, r = 0, g = 0, b = 0, a = 0;

            for (int dy = 0; dy < N; ++dy)
                for(int dx = 0; dx < N; ++dx)
                {
                    int sx = x - dx;
                    if (sx < 0) sx += outsize.width;

                    int sy = y - dy;
                    if (sy < 0) sy += outsize.height;

                    if (OnBoundary(sx, sy)) continue;

                    w = wave[sx, sy];
                    for (int t = 0; t < T; ++t)
                    {
                        if (w[t])
                        {
                            contributors++;
                            byte idx = patterns[t].Get(dx, dy);

                            Color c = attributes.ShowMaskWithAlphaValue 
                                ? Color.clear : Color.white;
                            if (idx != mask_idx)
                                c = colors[patterns[t].Get(dx, dy)];
                            r += c.r;
                            g += c.g;
                            b += c.b;
                            a += c.a;
                        }
                    }
                }

            if (contributors != 0)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                Color newCol = new Color(r / contributors, g / contributors, b / contributors, a / contributors);

                // An attemp at changing only the relevant tile (i.e. change it only if the color is different)
                if (output.GetColor(pos) != newCol)
                {
                    Tile t = GameObject.Instantiate(blanktile);
                    t.color = newCol;
                    output.SetTile(pos, t);
                }
            }
        });
    }

    /// <summary>
    /// Return the list of pattern that are possible for the selected position
    /// </summary>
    public Dictionary<int, Texture2D> GetPatternsForWave(Vector2Int p)
    {
        Dictionary<int, Texture2D> res = new Dictionary<int, Texture2D>();

        bool[] w = wave[p.x, p.y];
        for (int t = 0; t < T; ++t)
        {
            if (w[t])
                res.Add(t, patterns[t].Print(colors));
        }

        return res;
    }


}
