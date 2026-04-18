namespace KayCare.Core.DTOs.Billing;

public class RevenueDashboardResponse
{
    // Headline metrics
    public decimal TotalInvoiced      { get; set; }
    public decimal TotalCollected     { get; set; }
    public decimal TotalOutstanding   { get; set; }
    public decimal TotalDiscounts     { get; set; }
    public decimal TotalAdjustments   { get; set; }
    public decimal TotalWrittenOff    { get; set; }
    public int     TotalBills         { get; set; }
    public int     OutstandingBills   { get; set; }
    public int     OverdueBills       { get; set; }   // Issued/PartiallyPaid > 30 days

    // Monthly revenue — last 6 months
    public List<MonthlyRevenuePoint> MonthlyRevenue { get; set; } = [];

    // By payer
    public List<PayerRevenueRow> ByPayer { get; set; } = [];

    // By status counts
    public List<StatusCount> ByStatus { get; set; } = [];
}

public class MonthlyRevenuePoint
{
    public string  Month      { get; set; } = string.Empty;  // "Jan 2026"
    public decimal Invoiced   { get; set; }
    public decimal Collected  { get; set; }
}

public class PayerRevenueRow
{
    public string  PayerName  { get; set; } = string.Empty;
    public int     BillCount  { get; set; }
    public decimal Invoiced   { get; set; }
    public decimal Collected  { get; set; }
    public decimal Outstanding { get; set; }
}

public class StatusCount
{
    public string Status { get; set; } = string.Empty;
    public int    Count  { get; set; }
    public decimal Total { get; set; }
}
