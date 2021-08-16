using RimWorld;

namespace Hobbits
{
    [DefOf]
    public static class ThoughtDefOf
    {
        public static ThoughtDef ObservedPartyTree;

        static ThoughtDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThoughtDefOf));
        }
    }
}