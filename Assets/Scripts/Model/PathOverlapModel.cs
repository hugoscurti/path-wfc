using System;
using System.Collections.Generic;

using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

using Utilities;

class PathOverlapModel
{
    int N;  //Edge size of pattern
    int T;  //Total number of unique patterns
    int overlap_N; //Maximum overlap size

    // TODO: Use something more abstract than tilemap, if we'd want to port this to other types of maps
    Tilemap input, output;

    RectInt outsize;
    RectInt insize;

    // We define only three colors
    public static Color freespace = Color.white,
        obstacle = new Color(165f / 255, 42f / 255, 42f / 255),
        path = Color.black;

    // Index associated with colors
    static byte freespace_idx = 0,
        obstacle_idx = 1,
        path_idx = 2;

    Pattern[] patterns;

    bool[][] wave;
    double[] stationary;
    byte[,] output_idx;
    bool observed;

    int[,][][] propagator;  //TODO: Might have to change this?

    bool[] changes;
    Stack<int> indexstack;

    System.Random random;

    double[] logProb; //Log prob for each pattern
    double logT;

    bool periodicIn = true,
        periodicOut = true;


    #region Utility Functions

    /// <summary>
    /// TODO: Maybe change this into a dict or something
    /// </summary>
    private static byte GetColorIndex(Color c)
    {
        if (c == freespace) return freespace_idx;
        else if (c == obstacle) return obstacle_idx;
        else if (c == path) return path_idx;
        else throw new NotSupportedException("Color must be one of the three colors specified");
    }

    private static Color GetColorFromIndex(byte index)
    {
        if (index == freespace_idx) return freespace;
        else if (index == obstacle_idx) return obstacle;
        else if (index == path_idx) return path;
        else throw new NotSupportedException("Index must match one of the color indices specified");
    }

    /// <summary>
    /// Convert color into indices.
    /// Since we know exactly the expected color, we can hard-code the color indices
    /// and look for them.
    /// </summary>
    private byte[,] IndexColours(Tilemap source, RectInt size)
    {
        byte[,] indices = new byte[size.width, size.height];

        for (int y = 0; y < indices.GetLength(1); ++y)
            for(int x = 0; x < indices.GetLength(0); ++x)
                indices[x, y] = GetColorIndex(source.GetColor(new Vector3Int(x, y, 0)));

        return indices;
    }

    /// <summary>
    /// Put an index on the stack
    /// </summary>
    private void Change(int i)
    {
        if (changes[i]) return;

        indexstack.Push(i);
        changes[i] = true;
    }


    private void FillLogs()
    {
        logT = Math.Log(T);
        logProb = new double[T];
        for (int t = 0; t < T; t++)
            logProb[t] = Math.Log(stationary[t]);
    }


    private bool OnBoundary(int i)
    {
        return OnBoundary(i, i % outsize.width, i / outsize.width);
    }

    private bool OnBoundary(int x, int y)
    {
        return OnBoundary(x + y * outsize.width, x, y);
    }

    private bool OnBoundary(int i, int x, int y)
    {
        // TODO: Add a boundary as a pattern that contains an obstacle and no pattern can fit into it?
        return !periodicOut &&
            (x + N > outsize.width || y + N > outsize.height);
    }

    private int GetSecondIndex(int i1, int di, int length)
    {
        int i2 = i1 + di;
        if (i2 < 0) i2 += length;
        else if (i2 >= length) i2 -= length;
        return i2;
    }


    #endregion

    #region Contructor

    public PathOverlapModel(Tilemap input, Tilemap output, int N, bool periodicInput, bool periodicOutput)
    {
        this.N = N;
        this.overlap_N = 2 * N - 1;
        this.input = input;
        this.output = output;
        this.periodicIn = periodicInput;
        this.periodicOut = periodicOutput;

        // We use outsize to get the size of the bitmap image to prevent issues when accessing bitmap images across multiple threads
        this.insize = new RectInt(0, 0, input.cellBounds.size.x, input.cellBounds.size.y);
        this.outsize = new RectInt(0, 0, output.cellBounds.size.x, output.cellBounds.size.y);

        byte[,] indices = IndexColours(input, insize);
        output_idx = IndexColours(output, outsize);

        int outflatsize = outsize.width * outsize.height;
        wave = new bool[outflatsize][];

        changes = new bool[outflatsize];
        indexstack = new Stack<int>();

        int C = 3; // We know there is only 3 colors possible

        // Generate all patterns
        // Store them using a unique identifier
        Dictionary<long, int> patternCounts = new Dictionary<long, int>();
        Dictionary<long, Pattern> patternDict = new Dictionary<long, Pattern>();

        Pattern temp;
        long index;

        //TODO: add periodic input and output?
        for (int y = 0; y < (periodicIn ? insize.height : insize.height - N + 1); ++y)
            for (int x = 0; x < (periodicIn ? insize.width : insize.width - N + 1); ++x)
            {
                temp = new Pattern(N, x, y, indices);

                // Filter out patterns that don't contain a path
                //if (!temp.ContainsColor(path_idx)) continue;

                // Filter out patterns that only has free space
                //if (temp.ContainsOnly(freespace_idx)) continue;

                index = temp.GetIndex(C);

                //TODO: Extend with reflexions, rotations, etc..
                if (patternCounts.ContainsKey(index))
                {
                    //Don't add up for freespace, we want it to be only one
                    if (temp.ContainsOnly(freespace_idx)) continue;

                    patternCounts[index]++;
                }
                else
                {
                    patternCounts.Add(index, 1);
                    patternDict.Add(index, temp);
                }
            }

        // Set propagators and other things
        T = patternCounts.Count;    //Number of different patterns from input
        patterns = new Pattern[T];
        stationary = new double[T]; //This is basically counts for patterns
        propagator = new int[overlap_N, overlap_N][][];

        int idx = 0;

        foreach (long key in patternCounts.Keys)
        {
            patterns[idx] = patternDict[key];
            stationary[idx] = patternCounts[key];
            idx++;
        }

        // Initialize wave array
        for (int i = 0; i < wave.Length; ++i)
            wave[i] = new bool[T];

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
    /// </summary>
    public bool PatternFits(int i, Pattern p)
    {
        int x = i % outsize.width,
            y = i / outsize.width;
        byte patt_col, out_col;

        for (int dx = 0; dx < N; ++dx)
            for (int dy = 0; dy < N; ++dy)
            {
                patt_col = p.Get(dx, dy);
                out_col = output_idx[(x + dx) % outsize.width, (y + dy) % outsize.height];

                // See if pattern matches the area represented by index i.
                // Matches means 
                if (patt_col == path_idx) {
                    // If there is a path in the pattern, output should have a free space
                    if (out_col != freespace_idx) return false;
                } else {
                    if (out_col != patt_col) return false;
                }
            }
        return true;
    }

    /// <summary>
    /// Reset wave and changes
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < wave.Length; i++)
        {
            if (OnBoundary(i)) continue;

            for (int t = 0; t < T; t++)
                // Filter out patterns that doesn't fit on the output
                wave[i][t] = PatternFits(i, patterns[t]);

            changes[i] = false;
        }
    }

    /// <summary>
    /// Clear stuff and prepare object for new execution
    /// </summary>
    public void Init(int seed)
    {
        observed = false;
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

    /// <summary>
    /// Gets all the tiles that have fixed pattern (i.e. one possible pattern) and propagates them)
    /// </summary>
    public void PropagateFixedWaves(bool propagate)
    {
        for (int i = 0; i < wave.Length; ++i)
        {
            if (OnBoundary(i)) continue;
            bool[] w = wave[i];

            if (w.Count(t => t) == 1)
                Change(i);
        }

        if (propagate)
            Propagate();
    }

    /// <summary>
    /// Calculate entropy for each pattern, for each position in output
    /// </summary>
    public bool? Observe()
    {
        double minEnt = 1e+3;
        int indexminEnt = -1;

        double minCount = Double.PositiveInfinity;
        int indexminCount = -1;

        for (int i = 0; i < wave.Length; ++i)
        {
            if (OnBoundary(i)) continue;

            bool[] w = wave[i];
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
            //if (sum == 0) return false; //We will not fill the whole area, and it is highly possible that some places cannot be filled (such as inside of obstacles)
            // therefore, we must not stop when we encounter a sum of 0
            if (sum == 0) continue;

            // Calculate entropy
            double noise = 1e-6 * random.NextDouble();
            double entropy = CalculateEntropy(w, amount, sum);
                
            if (amount > 1 && amount + noise < minCount)
            {
                minCount = amount + noise;
                indexminCount = i;
            }

            // Store as min if smaller than min
            // For equal values, we use a small random value to decide which one to use
            if (entropy > 0 && entropy + noise < minEnt)
            {
                minEnt = entropy + noise;
                indexminEnt = i;
            }
        }

        // If we haven't found a min, we fill the observed array.
        //TODO: think of another way to output result?
        if (indexminEnt == -1)
        {
            observed = true;
            // Returning true means the algorithm converged
            return true;
        }

        double[] distribution = new double[T];
        for (int t = 0; t < T; t++)
            distribution[t] = wave[indexminEnt][t] ? stationary[t] : 0;

        // Choose one pattern 
        int r = distribution.Random(random.NextDouble());

        for (int t = 0; t < T; t++)
            // Set one pattern r in T to be observed. Set others to false
            wave[indexminEnt][t] = t == r;

        Change(indexminEnt);

        // Algorithm is not finished yet
        return null;
    }

    public void Propagate()
    {
        while (indexstack.Count > 0)
        {
            int i1 = indexstack.Pop();
            changes[i1] = false;

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

        int i1;
        i1 = indexstack.Pop();
        changes[i1] = false;

        _Propagate(i1);

        return true;
    }

        
    private void _Propagate(int i1)
    {
        bool[] w1 = wave[i1];

        //Extrapolate x and y coordinates based on the one-dimensionnal index i1
        int x1 = i1 % outsize.width, y1 = i1 / outsize.width;

        if (w1.All(w => !w)) {
            // For debug purposes
        }

        // Iterate over all overlap combinations
        for (int dx = -N + 1; dx < N; ++dx)
            for (int dy = -N + 1; dy < N; ++dy)
            {
                int x2 = GetSecondIndex(x1, dx, outsize.width),
                    y2 = GetSecondIndex(y1, dy, outsize.height);

                // If on boundary
                if (OnBoundary(x2, y2)) continue;

                int i2 = x2 + y2 * outsize.width;
                bool[] w2 = wave[i2];
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
                        Change(i2);
                    }

                    if (w2.All(w => !w))
                        return;
                }
            }
    }

    public void Print(Tile blanktile)
    {
        Color[] data = new Color[wave.Length];

        // Accumulate all possible values remaining for each pixel
        for (int i = 0; i < wave.Length; ++i)
        {
            float contributors = 0, r = 0, g = 0, b = 0;
            int x = i % outsize.width, y = i / outsize.width;

            for (int dy = 0; dy < N; ++dy)
                for (int dx = 0; dx < N; ++dx)
                {
                    int sx = x - dx;
                    if (sx < 0) sx += outsize.width;

                    int sy = y - dy;
                    if (sy < 0) sy += outsize.width;

                    int s = sx + sy * outsize.width;
                    if (OnBoundary(s)) continue;

                    for (int t = 0; t < T; ++t)
                    {
                        if (wave[s][t])
                        {
                            contributors++;
                            Color c = GetColorFromIndex(patterns[t].Get(dx, dy));
                            r += c.r;
                            g += c.g;
                            b += c.b;
                        }
                    }
                }

            if (contributors == 0)
            // Output the original image's pixel value
            { }
            else
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                Tile t = GameObject.Instantiate(blanktile);
                t.color = new Color(r / contributors, g / contributors, b / contributors);

                output.SetTile(pos, t);
            }
        }
    }



    #region Bitmap Functions

    //private int ToBitmapData(Color c)
    //{
    //    return unchecked((c.A << 24) | (c.R << 16) | (c.G << 8) | (c.B));
    //}

    //private int AverageToBitmapData(int red, int green, int blue, int contribs)
    //{
    //    return unchecked(
    //        (int)0xff000000 | ((red / contribs) << 16) | ((green / contribs) << 8) | blue / contribs);
    //}

    //private Bitmap GetBitmapFromData(int[] bitmapData, int width, int height)
    //{
    //    Bitmap result = new Bitmap(width, height);
    //    var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height),
    //        ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

    //    System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
    //    result.UnlockBits(bits);
    //    return result;
    //}

    //public Bitmap[] PrintPatterns()
    //{
    //    Bitmap[] res = new Bitmap[T];
    //    int[] bitmapData = new int[N * N];
    //    Color c;
    //    Pattern p;

    //    for (int i = 0; i < T; ++i)
    //    {
    //        p = patterns[i];
    //        for (int x = 0; x < N; ++x)
    //            for (int y = 0; y < N; ++y)
    //            {
    //                c = GetColorFromIndex(p.Get(x, y));
    //                bitmapData[x + y * N] = ToBitmapData(c);
    //            }

    //        res[i] = GetBitmapFromData(bitmapData, N, N);
    //    }

    //    return res;
    //}

    //public Bitmap[] PrintOverlaps(int p1)
    //{
    //    List<Bitmap> results = new List<Bitmap>();

    //    // 1. Fill the list of overlaps
    //    for (int x = 0; x < overlap_N; ++x)
    //        for (int y = 0; y < overlap_N; ++y)
    //        {
    //            int dx = x - N + 1,
    //                dy = y - N + 1;

    //            foreach (int p2 in propagator[x, y][p1])
    //                results.Add(PrintOverlap(p1, p2, dx, dy));
    //        }

    //    // 2. Return the list of bitmap
    //    return results.ToArray();
    //}

    //private Bitmap PrintOverlap(int i1, int i2, int dx, int dy)
    //{
    //    Pattern p1 = patterns[i1],
    //        p2 = patterns[i2];
    //    int xmax = N + Math.Abs(dx),
    //        ymax = N + Math.Abs(dy);
    //    int p2xmin = dx, p2xmax = dx + N,
    //        p2ymin = dy, p2ymax = dy + N; 

    //    int[] bitmapData = new int[xmax * ymax];
    //    for (int i = 0; i < bitmapData.Length; ++i)
    //        //Default color
    //        bitmapData[i] = ToBitmapData(Color.gray);

    //    int bx, by;

    //    // P1
    //    for (int x = 0; x < N; ++x)
    //        for (int y = 0; y < N; ++y)
    //        {
    //            bx = dx < 0 ? x - dx : x;
    //            by = dy < 0 ? y - dy : x;

    //            Color c = GetColorFromIndex(p1.Get(x, y));
    //            bitmapData[by * N + bx] = ToBitmapData(c);
    //        }

    //    for (int x = 0; x < N; ++x)
    //        for (int y = 0; y < N; ++y)
    //        {
    //            bx = dx < 0 ? x : x + dx;
    //            by = dy < 0 ? y : y + dy;

    //            Color c = Color.FromArgb(128, GetColorFromIndex(p2.Get(x, y)));
    //            bitmapData[by * N + bx] = ToBitmapData(c);
    //        }

    //    return GetBitmapFromData(bitmapData, xmax, ymax);
    //}

    //public Bitmap Graphics()
    //{
    //    int[] bitmapData = new int[output.Width * output.Height];

    //    // Accumulate all possible values remaining for each pixel
    //    for (int i = 0; i < wave.Length; ++i)
    //    {
    //        int contributors = 0, r = 0, g = 0, b = 0;
    //        int x = i % output.Width, y = i / output.Width;

    //        for (int dy = 0; dy < N; ++dy)
    //            for (int dx = 0; dx < N; ++dx)
    //            {
    //                int sx = x - dx;
    //                if (sx < 0) sx += output.Width;

    //                int sy = y - dy;
    //                if (sy < 0) sy += output.Width;

    //                int s = sx + sy * output.Width;
    //                if (OnBoundary(s)) continue;

    //                for (int t = 0; t < T; ++t)
    //                {
    //                    if (wave[s][t])
    //                    {
    //                        contributors++;
    //                        Color c = GetColorFromIndex(patterns[t].Get(dx, dy));
    //                        r += c.R;
    //                        g += c.G;
    //                        b += c.B;
    //                    }
    //                }
    //            }

    //        if (contributors == 0)
    //            // Output the original image's pixel value
    //            bitmapData[i] = ToBitmapData(output.GetPixel(x, y));
    //        else
    //            bitmapData[i] = AverageToBitmapData(r, g, b, contributors);
    //    }

    //    // Construct bitmap using bitmap data
    //    return GetBitmapFromData(bitmapData, output.Width, output.Height);
    //}


    #endregion


}
