using System;

namespace ProjectZ
{
    /// <summary>
    /// Canonical random drop roll used when normal bushes and grass are removed.
    /// The caller owns the random source so lightweight renderers do not advance
    /// the running game's global random sequence.
    /// </summary>
    public static class BushDropRules
    {
        public const string HeartItemName = "heart";
        public const string RupeeItemName = "ruby";

        public static string Roll(
            Func<int, int, int> next,
            bool noHeartDrops = false)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            // ObjBush has a 1-in-8 item chance followed by an even heart/rupee
            // split. Keep the two calls separate so gameplay's Random sequence
            // remains identical to the original implementation.
            if (next(0, 8) != 0)
                return null;

            var itemName = next(0, 2) == 0
                ? HeartItemName
                : RupeeItemName;
            return noHeartDrops && itemName == HeartItemName
                ? null
                : itemName;
        }
    }
}
