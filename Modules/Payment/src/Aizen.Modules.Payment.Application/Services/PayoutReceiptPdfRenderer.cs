using Aizen.Modules.Payment.Domain.Entities.Payout;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Aizen.Modules.Payment.Application.Services;

public interface IPayoutReceiptPdfRenderer
{
    byte[] Render(PayoutRecordEntity payout);
}

public sealed class PayoutReceiptPdfRenderer : IPayoutReceiptPdfRenderer
{
    public byte[] Render(PayoutRecordEntity payout)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("ÖDEME DEKONTU").Bold().FontSize(20);
                    col.Item().PaddingTop(5).Text($"Durum: {payout.Status}").FontSize(10);
                    col.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Item().Text($"Tutar: {payout.Amount:N2} {payout.CurrencyCode}").Bold().FontSize(16);
                    col.Item().PaddingTop(15).Text("Ödeme Bilgileri").Bold().FontSize(12);

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(180);
                            cols.RelativeColumn();
                        });

                        AddRow(table, "Ödeme Sağlayıcı", payout.GatewayProvider);
                        if (!string.IsNullOrEmpty(payout.GatewayPayoutId))
                            AddRow(table, "Referans No", payout.GatewayPayoutId);
                        if (!string.IsNullOrEmpty(payout.SourceType))
                            AddRow(table, "Kaynak", $"{payout.SourceType} (ID: {payout.SourceId})");
                        if (!string.IsNullOrEmpty(payout.Description))
                            AddRow(table, "Açıklama", payout.Description);
                        AddRow(table, "Talep Tarihi", payout.RequestedAt.ToString("dd.MM.yyyy HH:mm"));
                        if (payout.ProcessedAt.HasValue)
                            AddRow(table, "İşlem Tarihi", payout.ProcessedAt.Value.ToString("dd.MM.yyyy HH:mm"));
                        AddRow(table, "Para Birimi", payout.CurrencyCode);
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Inktavia Marine OS — Ödeme Dekontu").FontSize(8);
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(4).Text(label).Bold().FontSize(9);
        table.Cell().Padding(4).Text(value).FontSize(9);
    }
}
