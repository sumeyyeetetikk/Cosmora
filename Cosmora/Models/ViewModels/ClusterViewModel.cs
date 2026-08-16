namespace Cosmora.Models.ViewModels;

public class ClusterViewModel
{
    public int K { get; set; }
    public int CityCount { get; set; }

    public List<CityClusterRow> Cities { get; set; } = new();
    public List<ClusterSummary> Clusters { get; set; } = new();
}

public class CityClusterRow
{
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public int Cluster { get; set; }
    public double AvgDailySales { get; set; }   
    public double TotalVolume { get; set; }     
    public double PeakDaySales { get; set; }   
}

public class ClusterSummary
{
    public int Cluster { get; set; }
    public int CityCount { get; set; }
    public double AvgDailySales { get; set; }
    public double TotalVolume { get; set; }
    public double PeakDaySales { get; set; }
    public string Label { get; set; } = "";
    public List<string> CityNames { get; set; } = new();
}