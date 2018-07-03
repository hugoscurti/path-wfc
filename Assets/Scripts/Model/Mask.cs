using System.Linq;

public class Mask
{
    // Unique index related to the mask
    public int index;

    // The type of mask.
    // (Either everything except the colors defined in set or simply the colors in set)
    public bool everythingExcept;

    // The colors that it accepts
    public int[] colors;

    public Mask(bool everythingExcept, params int[] colors)
    {
        this.everythingExcept = everythingExcept;
        this.colors = colors;
    }

    public bool Agrees(int idx)
    {
        if (everythingExcept)
            return colors.All(i => idx != i);
        else
            return colors.Any(i => idx == i);
    }

}
