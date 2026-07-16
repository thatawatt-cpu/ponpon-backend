namespace PonPon.Api.Options;

public sealed class DashboardCacheOptions
{
    public const string SectionName = "DashboardCache";

    public bool Enabled { get; set; } = true;
    public int DaySeconds { get; set; } = 20;
    public int WeekSeconds { get; set; } = 60;
    public int MonthSeconds { get; set; } = 120;
    public int YearSeconds { get; set; } = 300;
    public int ShippingSeconds { get; set; } = 15;
}
