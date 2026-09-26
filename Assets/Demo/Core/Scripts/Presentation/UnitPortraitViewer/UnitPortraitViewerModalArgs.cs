namespace Demo.Core.Scripts.Presentation.UnitPortraitViewer
{
    public sealed class UnitPortraitViewerModalArgs
    {
        public UnitPortraitViewerModalArgs(string unitTypeMasterId, int unitRank)
        {
            UnitTypeMasterId = unitTypeMasterId;
            UnitRank = unitRank;
        }

        public string UnitTypeMasterId { get; }
        public int UnitRank { get; }
    }
}
