using System;

namespace ChessTrainerApp
{
    [Flags]
    public enum CastlingOptions
    {
        /// <summary>
        /// May not castle long or short
        /// </summary>
        None = 0,

        /// <summary>
        /// May castle short (king-side)
        /// </summary>
        KingSide = 1,

        /// <summary>
        /// May castle long (queen-side)
        /// </summary>
        QueenSide = 2
    }
}
