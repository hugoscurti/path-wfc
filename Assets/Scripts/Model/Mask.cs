using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Mask
{
    // Unique index related to the mask
    public int index;

    // The type of mask.
    // (Either everything except the colors defined in set or simply the colors in set)
    public bool everythingExcept;

    // The colors that it accepts
    public HashSet<int> colors;


    // TODO: add utility functions to compare mask with color, or others...

}
