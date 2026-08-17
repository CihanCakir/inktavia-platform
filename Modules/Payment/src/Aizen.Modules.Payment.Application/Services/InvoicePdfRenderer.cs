using Aizen.Modules.Payment.Domain.Entities.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Aizen.Modules.Payment.Application.Services;

public interface IInvoicePdfRenderer
{
    byte[] Render(InvoiceHeaderEntity invoice);
}

public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    public byte[] Render(InvoiceHeaderEntity invoice)
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
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("FATURA").Bold().FontSize(18);
                            if (!string.IsNullOrEmpty(invoice.InvoiceNumber))
                                text.Span($"  #{invoice.InvoiceNumber}").FontSize(12);
                        });
                        row.ConstantItem(120).AlignRight().Text(text =>
                        {
                            text.Span($"Durum: {invoice.Status}").FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Satici").Bold().FontSize(9);
                            c.Item().Text(invoice.SellerName);
                            if (!string.IsNullOrEmpty(invoice.SellerTaxNumber))
                                c.Item().Text($"VKN: {invoice.SellerTaxNumber}").FontSize(8);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Alici").Bold().FontSize(9);
                            c.Item().Text(invoice.BuyerName);
                            if (!string.IsNullOrEmpty(invoice.BuyerTaxNumber))
                                c.Item().Text($"VKN: {invoice.BuyerTaxNumber}").FontSize(8);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Tarihler").Bold().FontSize(9);
                            if (invoice.IssueDateUtc.HasValue)
                                c.Item().Text($"Duzenleme: {invoice.IssueDateUtc.Value:dd.MM.yyyy}").FontSize(8);
                            if (invoice.DueDateUtc.HasValue)
                                c.Item().Text($"Vade: {invoice.DueDateUtc.Value:dd.MM.yyyy}").FontSize(8);
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.ConstantColumn(60);
                            cols.ConstantColumn(80);
                            cols.ConstantColumn(80);
                            cols.ConstantColumn(60);
                            cols.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Aciklama").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Miktar").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Birim Fiyat").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Satir Toplami").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("KDV %").Bold().FontSize(9);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("KDV Tutar").Bold().FontSize(9);
                        });

                        foreach (var line in invoice.Lines)
                        {
                            table.Cell().Padding(4).Text(line.Description).FontSize(9);
                            table.Cell().Padding(4).AlignRight().Text(line.Quantity.ToString("N2")).FontSize(9);
                            table.Cell().Padding(4).AlignRight().Text($"{line.UnitPrice:N2} {invoice.Currency}").FontSize(9);
                            table.Cell().Padding(4).AlignRight().Text($"{line.LineTotal:N2}").FontSize(9);
                            table.Cell().Padding(4).AlignRight().Text($"{line.TaxRate * 100:N0}%").FontSize(9);
                            table.Cell().Padding(4).AlignRight().Text($"{line.TaxAmount:N2}").FontSize(9);
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Width(250).Column(totals =>
                    {
                        TotalRow(totals, "Ara Toplam", invoice.SubTotalAmount, invoice.Currency);
                        if (invoice.DiscountAmount != 0)
                            TotalRow(totals, "Indirim", -invoice.DiscountAmount, invoice.Currency);
                        TotalRow(totals, "KDV", invoice.TaxAmount, invoice.Currency);
                        totals.Item().LineHorizontal(1);
                        TotalRow(totals, "Genel Toplam", invoice.TotalAmount, invoice.Currency, bold: true);
                        if (invoice.PaidAmount > 0)
                            TotalRow(totals, "Odenen", invoice.PaidAmount, invoice.Currency);
                        if (invoice.RemainingAmount > 0)
                            TotalRow(totals, "Kalan", invoice.RemainingAmount, invoice.Currency);
                    });

                    if (!string.IsNullOrEmpty(invoice.Notes))
                    {
                        col.Item().PaddingTop(20).Text("Notlar:").Bold().FontSize(9);
                        col.Item().Text(invoice.Notes).FontSize(9);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Inktavia Marine OS — ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" / ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void TotalRow(ColumnDescriptor col, string label, decimal amount, string currency, bool bold = false)
    {
        col.Item().Row(row =>
        {
            var text1 = row.RelativeItem().AlignLeft().Text(label).FontSize(9);
            var text2 = row.ConstantItem(100).AlignRight().Text($"{amount:N2} {currency}").FontSize(9);
            if (bold) { text1.Bold(); text2.Bold(); }
        });
    }
}
