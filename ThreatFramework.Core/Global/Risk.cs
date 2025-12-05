namespace ThreatModeler.TF.Core.Global
{
    public sealed class Risk
    {
        public string Name { get; set; } = string.Empty;
        public required string Color { get; set; }
        public required string SuggestedName { get; set; }
        public int Score { get; set; }
        public required string ChineseName { get; set; }
    }
}
