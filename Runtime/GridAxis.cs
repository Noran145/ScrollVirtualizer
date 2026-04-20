namespace NoranDev.ScrollVirtualizer
{
    /// <summary>
    /// Grid start axis
    /// </summary>
    public enum GridAxis
    {
        /// <summary>
        /// Vertical
        /// </summary>
        Vertical,

        /// <summary>
        /// Horizontal
        /// </summary>
        Horizontal,
    }

    /// <summary>
    /// Grid start corner
    /// </summary>
    public enum GridStartCorner
    {
        UpperLeft,
        UpperRight,
        LowerLeft,
        LowerRight
    }

    /// <summary>
    /// Grid constraint type
    /// </summary>
    public enum GridConstraint
    {
        Flexible,
        FixedColumnCount,
        FixedRowCount
    }

    /// <summary>
    /// Grid child alignment
    /// </summary>
    public enum GridChildAlignment
    {
        UpperLeft,
        UpperCenter,
        UpperRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        LowerLeft,
        LowerCenter,
        LowerRight
    }
}
