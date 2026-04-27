namespace ClineTools.Modules.PointDetail
{
    public sealed class PointDetailRequest
    {
        public string Type { get; set; } = string.Empty;
        public double DiameterValue { get; set; }
        public string Unit { get; set; } = "in"; // "in" or "mm"
    }
}