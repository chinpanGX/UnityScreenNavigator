namespace Demo.Core.Scripts.Presentation.UnitTypeInformation
{
    public sealed class UnitTypeInformationModalArgs
    {
        public UnitTypeInformationModalArgs(string unitTypeMasterId)
        {
            UnitTypeMasterId = unitTypeMasterId;
        }

        public string UnitTypeMasterId { get; }
    }
}
