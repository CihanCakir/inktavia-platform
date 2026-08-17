namespace Aizen.Bff.AdminPanel.Application.Dashboard.Dto;

[DocumentationInfo("Monthly revenue/commission DTO",
    "One month bucket (Month = yyyy-MM, UTC) of revenue + commission for the admin dashboard revenue chart (C1).")]
public sealed class MonthlyRevenueCommissionDto
{
    public string Month { get; set; } = default!;
    public decimal Revenue { get; set; }
    public decimal Commission { get; set; }
}
