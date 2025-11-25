namespace AdminBFF.Models;

/// <summary>
/// Dashboard aggregated data
/// </summary>
public class DashboardDto
{
    public UserStats Users { get; set; } = new();
    public OrderStats Orders { get; set; } = new();
    public ProductStats Products { get; set; } = new();
    public RevenueStats Revenue { get; set; } = new();
}

public class UserStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int NewToday { get; set; }
}

public class OrderStats
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int CompletedToday { get; set; }
}

public class ProductStats
{
    public int Total { get; set; }
    public int LowStock { get; set; }
    public int OutOfStock { get; set; }
}

public class RevenueStats
{
    public decimal Today { get; set; }
    public decimal Week { get; set; }
    public decimal Month { get; set; }
}
