using Application.Contractors;
using Application.ReponseDTO;
using Application.RequestDTO;
using Domain.Entitites;
using System.Text;

namespace Application.Services
{
    public class PrintService : IPrintService
    {
        private readonly IPrintSettingsRepository _printSettingsRepository;

        public PrintService(IPrintSettingsRepository printSettingsRepository)
        {
            _printSettingsRepository = printSettingsRepository;
        }

        public async Task<PrintSettingsResponse> GetPrintSettingsByBranchAsync(int branchId, CancellationToken cancellationToken = default)
        {
            var printSettings = await _printSettingsRepository.GetByBranchIdAsync(branchId, cancellationToken);

            if (printSettings == null)
            {
                // Return default settings if none exist
                var branch = await _printSettingsRepository.GetBranchByIdAsync(branchId);
                return new PrintSettingsResponse
                {
                    BranchId = branchId,
                    BranchName = branch?.Name ?? "Main Branch",
                    CompanyName = "Your Company Name",
                    CompanyAddress = "123 Business Street, City, State",
                    CompanyPhone = "+1 (555) 123-4567",
                    CompanyEmail = "info@yourcompany.com",
                    ShowLogo = true,
                    ShowBranchInfo = true,
                    PaperSize = "A4",
                    Orientation = "portrait",
                    FontSize = "medium",
                    DefaultCopies = 1,
                    FooterText = "Thank you for your business!",
                    ThankYouMessage = "Payment received successfully!",
                    PaymentReceiptTitle = "PAYMENT RECEIPT",
                    ReceiptNumberPrefix = "PAY-"
                };
            }

            return new PrintSettingsResponse
            {
                Id = printSettings.Id,
                BranchId = printSettings.BranchId,
                BranchName = printSettings.Branch?.Name ?? "Unknown Branch",
                CompanyName = printSettings.CompanyName,
                CompanyAddress = printSettings.CompanyAddress,
                CompanyPhone = printSettings.CompanyPhone,
                CompanyEmail = printSettings.CompanyEmail,
                CompanyWebsite = printSettings.CompanyWebsite,
                LogoUrl = printSettings.LogoUrl,
                BranchAddress = printSettings.BranchAddress,
                BranchPhone = printSettings.BranchPhone,
                ShowLogo = printSettings.ShowLogo,
                ShowBranchInfo = printSettings.ShowBranchInfo,
                PaperSize = printSettings.PaperSize,
                Orientation = printSettings.Orientation,
                FontSize = printSettings.FontSize,
                DefaultCopies = printSettings.DefaultCopies,
                FooterText = printSettings.FooterText,
                TermsAndConditions = printSettings.TermsAndConditions,
                ThankYouMessage = printSettings.ThankYouMessage,
                AuthorizedBy = printSettings.AuthorizedBy,
                Signature = printSettings.Signature,
                ShowPaymentDetails = printSettings.ShowPaymentDetails,
                ShowSupplierInfo = printSettings.ShowSupplierInfo,
                ShowAmountSummary = printSettings.ShowAmountSummary,
                ShowNotes = printSettings.ShowNotes,
                ReceiptNumberPrefix = printSettings.ReceiptNumberPrefix,
                PaymentReceiptTitle = printSettings.PaymentReceiptTitle
            };
        }

        public async Task<PrintSettingsResponse> CreateOrUpdatePrintSettingsAsync(int branchId, PrintSettingsResponse settings, CancellationToken cancellationToken = default)
        {
            var existingSettings = await _printSettingsRepository.GetByBranchIdAsync(branchId, cancellationToken);

            if (existingSettings == null)
            {
                // Create new settings
                var newSettings = new PrintSettings
                {
                    BranchId = branchId,
                    CompanyName = settings.CompanyName,
                    CompanyAddress = settings.CompanyAddress,
                    CompanyPhone = settings.CompanyPhone,
                    CompanyEmail = settings.CompanyEmail,
                    CompanyWebsite = settings.CompanyWebsite,
                    LogoUrl = settings.LogoUrl,
                    BranchAddress = settings.BranchAddress,
                    BranchPhone = settings.BranchPhone,
                    ShowLogo = settings.ShowLogo,
                    ShowBranchInfo = settings.ShowBranchInfo,
                    PaperSize = settings.PaperSize,
                    Orientation = settings.Orientation,
                    FontSize = settings.FontSize,
                    DefaultCopies = settings.DefaultCopies,
                    FooterText = settings.FooterText,
                    TermsAndConditions = settings.TermsAndConditions,
                    ThankYouMessage = settings.ThankYouMessage,
                    AuthorizedBy = settings.AuthorizedBy,
                    Signature = settings.Signature,
                    ShowPaymentDetails = settings.ShowPaymentDetails,
                    ShowSupplierInfo = settings.ShowSupplierInfo,
                    ShowAmountSummary = settings.ShowAmountSummary,
                    ShowNotes = settings.ShowNotes,
                    ReceiptNumberPrefix = settings.ReceiptNumberPrefix,
                    PaymentReceiptTitle = settings.PaymentReceiptTitle
                };

                var createdSettings = await _printSettingsRepository.CreateAsync(newSettings, cancellationToken);
                settings.Id = createdSettings.Id;
            }
            else
            {
                // Update existing settings
                existingSettings.CompanyName = settings.CompanyName;
                existingSettings.CompanyAddress = settings.CompanyAddress;
                existingSettings.CompanyPhone = settings.CompanyPhone;
                existingSettings.CompanyEmail = settings.CompanyEmail;
                existingSettings.CompanyWebsite = settings.CompanyWebsite;
                existingSettings.LogoUrl = settings.LogoUrl;
                existingSettings.BranchAddress = settings.BranchAddress;
                existingSettings.BranchPhone = settings.BranchPhone;
                existingSettings.ShowLogo = settings.ShowLogo;
                existingSettings.ShowBranchInfo = settings.ShowBranchInfo;
                existingSettings.PaperSize = settings.PaperSize;
                existingSettings.Orientation = settings.Orientation;
                existingSettings.FontSize = settings.FontSize;
                existingSettings.DefaultCopies = settings.DefaultCopies;
                existingSettings.FooterText = settings.FooterText;
                existingSettings.TermsAndConditions = settings.TermsAndConditions;
                existingSettings.ThankYouMessage = settings.ThankYouMessage;
                existingSettings.AuthorizedBy = settings.AuthorizedBy;
                existingSettings.Signature = settings.Signature;
                existingSettings.ShowPaymentDetails = settings.ShowPaymentDetails;
                existingSettings.ShowSupplierInfo = settings.ShowSupplierInfo;
                existingSettings.ShowAmountSummary = settings.ShowAmountSummary;
                existingSettings.ShowNotes = settings.ShowNotes;
                existingSettings.ReceiptNumberPrefix = settings.ReceiptNumberPrefix;
                existingSettings.PaymentReceiptTitle = settings.PaymentReceiptTitle;

                await _printSettingsRepository.UpdateAsync(existingSettings, cancellationToken);
                settings.Id = existingSettings.Id;
            }

            return settings;
        }

        private string GetPrintStyles(PrintSettingsResponse settings)
        {
            return $@"
                <style>
                    @page {{
                        size: {GetPageSize(settings.PaperSize)};
                        margin: {GetPageMargin(settings.PaperSize)};
                        orientation: {settings.Orientation};
                    }}
                    body {{
                        font-family: 'Arial', sans-serif;
                        font-size: {GetFontSize(settings.FontSize)};
                        line-height: 1.4;
                        margin: 0;
                        padding: 0;
                        color: #333;
                    }}
                    .print-container {{
                        width: 100%;
                        height: 100%;
                        display: flex;
                        flex-direction: column;
                    }}
                    .print-header {{
                        text-align: center;
                        border-bottom: 2px solid #333;
                        padding-bottom: 10px;
                        margin-bottom: 20px;
                    }}
                    .print-header h1 {{
                        margin: 0;
                        font-size: {GetHeaderFontSize(settings.FontSize)};
                        font-weight: bold;
                    }}
                    .print-header .company-info {{
                        margin: 5px 0;
                        font-size: {GetSubHeaderFontSize(settings.FontSize)};
                    }}
                    .print-content {{
                        flex: 1;
                        padding: 10px 0;
                    }}
                    .print-footer {{
                        border-top: 1px solid #333;
                        padding-top: 10px;
                        margin-top: 20px;
                        text-align: center;
                        font-size: {GetFooterFontSize(settings.FontSize)};
                    }}
                    .receipt-table {{
                        width: 100%;
                        border-collapse: collapse;
                        margin: 15px 0;
                    }}
                    .receipt-table th, .receipt-table td {{
                        padding: 8px;
                        text-align: left;
                        border-bottom: 1px solid #ddd;
                    }}
                    .receipt-table th {{
                        background-color: #f8f9fa;
                        font-weight: bold;
                    }}
                    .amount-row {{
                        font-weight: bold;
                        background-color: #f8f9fa;
                    }}
                    .text-right {{ text-align: right; }}
                    .text-center {{ text-align: center; }}
                    .mb-2 {{ margin-bottom: 10px; }}
                    .mb-3 {{ margin-bottom: 15px; }}
                    .row {{
                        display: flex;
                        justify-content: space-between;
                        margin-bottom: 5px;
                    }}
                    .col-label {{
                        font-weight: bold;
                        width: 40%;
                    }}
                    .col-value {{
                        width: 60%;
                    }}
                    @media print {{
                        body {{
                            -webkit-print-color-adjust: exact;
                            print-color-adjust: exact;
                        }}
                        .no-print {{
                            display: none !important;
                        }}
                    }}
                </style>";
        }

        private string GetPageSize(string paperSize)
        {
            return paperSize switch
            {
                "A4" => "210mm 297mm",
                "A5" => "148mm 210mm",
                "RECEIPT" => "80mm auto",
                "THERMAL" => "58mm auto",
                _ => "210mm 297mm"
            };
        }

        private string GetPageMargin(string paperSize)
        {
            return paperSize.Contains("RECEIPT") || paperSize == "THERMAL" ? "5mm" : "15mm";
        }

        private string GetFontSize(string fontSize)
        {
            return fontSize switch
            {
                "small" => "12px",
                "large" => "16px",
                _ => "14px"
            };
        }

        private string GetHeaderFontSize(string fontSize)
        {
            return fontSize switch
            {
                "small" => "18px",
                "large" => "24px",
                _ => "20px"
            };
        }

        private string GetSubHeaderFontSize(string fontSize)
        {
            return fontSize switch
            {
                "small" => "10px",
                "large" => "14px",
                _ => "12px"
            };
        }

        private string GetFooterFontSize(string fontSize)
        {
            return fontSize switch
            {
                "small" => "8px",
                "large" => "12px",
                _ => "10px"
            };
        }

        public async Task<BookingInvoiceData> GetBookingInvoiceDataAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await _printSettingsRepository.GetBookingByIdAsync(bookingId, cancellationToken);

            if (booking == null)
                throw new ArgumentException($"Booking with ID {bookingId} not found.");

            var totalQuantity = booking.BookingDetails?.Sum(d => (decimal)d.BookingQuantity) ?? 0;
            var totalAmount = booking.BookingDetails?.Sum(d => (decimal)d.BookingQuantity * d.BookingRate) ?? 0;

            var invoiceData = new BookingInvoiceData
            {
                Id = booking.Id,
                BookingNumber = booking.BookingNumber,
                BookingDate = booking.BookingDate,
                Customer = new CustomerInfo
                {
                    Name = booking.Customer?.CustomerName ?? "Unknown Customer",
                    Phone = booking.Customer?.CustomerMobile ?? "",
                    Address = booking.Customer?.Address ?? "",
                    Email = booking.Customer?.CustomerEmail ?? ""
                },
                Branch = new BranchInfo
                {
                    Name = booking.Branch?.Name ?? "Main Branch",
                    Address = booking.Branch?.Address ?? "",
                    Phone = booking.Branch?.Phone ?? ""
                },
                Notes = booking.Notes,
                TotalAmount = totalAmount,
                TotalQuantity = totalQuantity,
                PrintDateTime = DateTime.UtcNow,
                CreatedBy = "System",
                BookingDetails = booking.BookingDetails?.Select((detail, index) => new BookingInvoiceDetail
                {
                    SerialNo = index + 1,
                    ProductName = detail.Product?.ProductName ?? "Unknown Product",
                    UnitName = detail.BookingUnit?.UnitName ?? detail.BookingUnit?.BaseUnit?.UnitName ?? "",
                    BookingQuantity = detail.BookingQuantity,
                    BillType = detail.BillType,
                    BookingRate = detail.BookingRate,
                    TotalAmount = (decimal)detail.BookingQuantity * detail.BookingRate,
                    BaseQuantity = detail.BaseQuantity,
                    BaseRate = detail.BaseRate,
                    DeliveryQuantity = 0
                }).ToList() ?? new List<BookingInvoiceDetail>()
            };

            return invoiceData;
        }

        public async Task<string> GenerateBookingInvoiceHtmlAsync(BookingInvoiceData bookingData, PrintSettingsResponse printSettings, CancellationToken cancellationToken = default)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine($"<title>Booking Invoice - {bookingData.BookingNumber}</title>");
            html.AppendLine(GetBookingInvoiceStyles());
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class='invoice-container'>");

            html.AppendLine(GenerateBookingInvoiceHeader(printSettings, bookingData));
            html.AppendLine(GenerateBookingInfo(bookingData));
            html.AppendLine(GenerateBookingDetailsTable(bookingData));
            html.AppendLine(GenerateBookingInvoiceFooter(printSettings, bookingData));

            html.AppendLine("</div>");

            // Add barcode generation script
            html.AppendLine("<script src='https://cdn.jsdelivr.net/npm/jsbarcode@3.11.5/dist/JsBarcode.all.min.js'></script>");
            html.AppendLine("<script>");
            html.AppendLine($"JsBarcode('#barcode-{bookingData.BookingNumber}', '{bookingData.BookingNumber}', {{");
            html.AppendLine("  format: 'CODE128',");
            html.AppendLine("  width: 2,");
            html.AppendLine("  height: 50,");
            html.AppendLine("  displayValue: false");
            html.AppendLine("});");
            html.AppendLine("</script>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GetBookingInvoiceStyles()
        {
            return @"
                <style>
                    @page {
                        size: A4;
                        margin: 10mm;
                    }
                    body {
                        font-family: 'Arial', sans-serif;
                        font-size: 12px;
                        line-height: 1.4;
                        margin: 0;
                        padding: 0;
                        color: #000;
                    }
                    .invoice-container {
                        width: 100%;
                        max-width: 210mm;
                        margin: 0 auto;
                        padding: 5mm;
                        border: 2px solid #000;
                    }
                    .invoice-header {
                        display: flex;
                        justify-content: space-between;
                        align-items: flex-start;
                        border-bottom: 3px solid #000;
                        padding-bottom: 10px;
                        margin-bottom: 15px;
                    }
                    .company-info {
                        flex: 1;
                        text-align: center;
                    }
                    .company-name {
                        font-size: 20px;
                        font-weight: bold;
                        margin: 5px 0;
                        text-transform: uppercase;
                    }
                    .company-address {
                        font-size: 11px;
                        margin: 3px 0;
                    }
                    .barcode-section {
                        text-align: right;
                        width: 150px;
                    }
                    .barcode-img {
                        max-width: 150px;
                        height: auto;
                    }
                    .booking-number {
                        font-size: 14px;
                        font-weight: bold;
                        text-align: center;
                        margin-top: 5px;
                    }
                    .info-section {
                        display: flex;
                        justify-content: space-between;
                        margin-bottom: 15px;
                        padding: 10px;
                        border: 1px solid #000;
                    }
                    .info-column {
                        flex: 1;
                    }
                    .info-row {
                        display: flex;
                        margin-bottom: 5px;
                    }
                    .info-label {
                        font-weight: bold;
                        min-width: 120px;
                    }
                    .info-value {
                        flex: 1;
                    }
                    .details-table {
                        width: 100%;
                        border-collapse: collapse;
                        margin-bottom: 15px;
                    }
                    .details-table th,
                    .details-table td {
                        border: 1px solid #000;
                        padding: 6px;
                        text-align: center;
                    }
                    .details-table th {
                        background-color: #f0f0f0;
                        font-weight: bold;
                        font-size: 11px;
                    }
                    .details-table td {
                        font-size: 11px;
                    }
                    .text-left { text-align: left !important; }
                    .text-right { text-align: right !important; }
                    .text-center { text-align: center !important; }
                    .footer-row {
                        font-weight: bold;
                        background-color: #f8f8f8;
                    }
                    .signature-section {
                        display: flex;
                        justify-content: space-between;
                        margin-top: 40px;
                        padding-top: 20px;
                    }
                    .signature-box {
                        text-align: center;
                        width: 200px;
                    }
                    .signature-line {
                        border-top: 1px solid #000;
                        margin-top: 50px;
                        padding-top: 5px;
                    }
                    .notes-section {
                        margin: 15px 0;
                        padding: 10px;
                        border: 1px solid #000;
                        background-color: #f9f9f9;
                    }
                    @media print {
                        body {
                            -webkit-print-color-adjust: exact;
                            print-color-adjust: exact;
                        }
                        .invoice-container {
                            border: 2px solid #000;
                        }
                    }
                </style>";
        }

        private string GenerateBookingInvoiceHeader(PrintSettingsResponse settings, BookingInvoiceData bookingData)
        {
            var header = new StringBuilder();
            header.AppendLine("<div class='invoice-header'>");

            if (settings.ShowLogo && !string.IsNullOrEmpty(settings.LogoUrl))
            {
                header.AppendLine("<div class='logo-section' style='width: 100px;'>");
                header.AppendLine($"<img src='{settings.LogoUrl}' alt='Logo' style='max-width: 80px; max-height: 80px;' />");
                header.AppendLine("</div>");
            }
            else
            {
                header.AppendLine("<div style='width: 100px;'></div>");
            }

            header.AppendLine("<div class='company-info'>");
            header.AppendLine($"<div class='company-name'>{settings.CompanyName}</div>");
            if (!string.IsNullOrEmpty(settings.CompanyAddress))
                header.AppendLine($"<div class='company-address'>{settings.CompanyAddress}</div>");
            if (!string.IsNullOrEmpty(settings.CompanyPhone))
                header.AppendLine($"<div class='company-address'>Phone: {settings.CompanyPhone}</div>");
            if (!string.IsNullOrEmpty(settings.CompanyEmail))
                header.AppendLine($"<div class='company-address'>Email: {settings.CompanyEmail}</div>");

            header.AppendLine("<div style='margin-top: 10px; font-size: 16px; font-weight: bold; text-decoration: underline;'>BOOKING INVOICE</div>");
            header.AppendLine("</div>");

            header.AppendLine("<div class='barcode-section'>");
            header.AppendLine($"<svg id='barcode-{bookingData.BookingNumber}' class='barcode-img'></svg>");
            header.AppendLine($"<div class='booking-number'>{bookingData.BookingNumber}</div>");
            header.AppendLine("</div>");

            header.AppendLine("</div>");
            return header.ToString();
        }

        private string GenerateBookingInfo(BookingInvoiceData bookingData)
        {
            var info = new StringBuilder();
            info.AppendLine("<div class='info-section'>");

            info.AppendLine("<div class='info-column'>");
            info.AppendLine("<div class='info-row'>");
            info.AppendLine("<span class='info-label'>Customer Name:</span>");
            info.AppendLine($"<span class='info-value'>{bookingData.Customer.Name}</span>");
            info.AppendLine("</div>");
            if (!string.IsNullOrEmpty(bookingData.Customer.Phone))
            {
                info.AppendLine("<div class='info-row'>");
                info.AppendLine("<span class='info-label'>Phone:</span>");
                info.AppendLine($"<span class='info-value'>{bookingData.Customer.Phone}</span>");
                info.AppendLine("</div>");
            }
            if (!string.IsNullOrEmpty(bookingData.Customer.Address))
            {
                info.AppendLine("<div class='info-row'>");
                info.AppendLine("<span class='info-label'>Address:</span>");
                info.AppendLine($"<span class='info-value'>{bookingData.Customer.Address}</span>");
                info.AppendLine("</div>");
            }
            info.AppendLine("</div>");

            info.AppendLine("<div class='info-column'>");
            info.AppendLine("<div class='info-row'>");
            info.AppendLine("<span class='info-label'>Booking Date:</span>");
            info.AppendLine($"<span class='info-value'>{bookingData.BookingDate:dd-MMM-yyyy}</span>");
            info.AppendLine("</div>");
            info.AppendLine("<div class='info-row'>");
            info.AppendLine("<span class='info-label'>Branch:</span>");
            info.AppendLine($"<span class='info-value'>{bookingData.Branch.Name}</span>");
            info.AppendLine("</div>");
            info.AppendLine("<div class='info-row'>");
            info.AppendLine("<span class='info-label'>Print Date:</span>");
            info.AppendLine($"<span class='info-value'>{bookingData.PrintDateTime:dd-MMM-yyyy HH:mm a}</span>");
            info.AppendLine("</div>");
            info.AppendLine("</div>");

            info.AppendLine("</div>");
            return info.ToString();
        }

        private string GenerateBookingDetailsTable(BookingInvoiceData bookingData)
        {
            var table = new StringBuilder();
            table.AppendLine("<table class='details-table'>");

            table.AppendLine("<thead>");
            table.AppendLine("<tr>");
            table.AppendLine("<th style='width: 30px;'>S.No</th>");
            table.AppendLine("<th style='width: 150px;'>Product Name</th>");
            table.AppendLine("<th style='width: 60px;'>Unit</th>");
            table.AppendLine("<th style='width: 80px;'>Bill Type</th>");
            table.AppendLine("<th style='width: 70px;'>Quantity</th>");
            table.AppendLine("<th style='width: 70px;'>Rate</th>");
            table.AppendLine("<th style='width: 80px;'>Amount</th>");
            table.AppendLine("<th style='width: 70px;'>Base Qty</th>");
            table.AppendLine("<th style='width: 70px;'>Delivery Qty</th>");
            table.AppendLine("</tr>");
            table.AppendLine("</thead>");

            table.AppendLine("<tbody>");
            foreach (var detail in bookingData.BookingDetails)
            {
                table.AppendLine("<tr>");
                table.AppendLine($"<td>{detail.SerialNo}</td>");
                table.AppendLine($"<td class='text-left'>{detail.ProductName}</td>");
                table.AppendLine($"<td>{detail.UnitName}</td>");
                table.AppendLine($"<td>{detail.BillType}</td>");
                table.AppendLine($"<td class='text-right'>{detail.BookingQuantity:F2}</td>");
                table.AppendLine($"<td class='text-right'>{detail.BookingRate:F2}</td>");
                table.AppendLine($"<td class='text-right'>{detail.TotalAmount:F2}</td>");
                table.AppendLine($"<td class='text-right'>{detail.BaseQuantity:F2}</td>");
                table.AppendLine($"<td class='text-right'>{detail.DeliveryQuantity:F2}</td>");
                table.AppendLine("</tr>");
            }
            table.AppendLine("</tbody>");

            table.AppendLine("<tfoot>");
            table.AppendLine("<tr class='footer-row'>");
            table.AppendLine("<td colspan='4' class='text-right'>Total:</td>");
            table.AppendLine($"<td class='text-right'>{bookingData.TotalQuantity:F2}</td>");
            table.AppendLine("<td></td>");
            table.AppendLine($"<td class='text-right'>{bookingData.TotalAmount:F2}</td>");
            table.AppendLine("<td></td>");
            table.AppendLine("<td></td>");
            table.AppendLine("</tr>");
            table.AppendLine("</tfoot>");

            table.AppendLine("</table>");

            if (!string.IsNullOrEmpty(bookingData.Notes))
            {
                table.AppendLine("<div class='notes-section'>");
                table.AppendLine($"<strong>Notes:</strong> {bookingData.Notes}");
                table.AppendLine("</div>");
            }

            return table.ToString();
        }

        private string GenerateBookingInvoiceFooter(PrintSettingsResponse settings, BookingInvoiceData bookingData)
        {
            var footer = new StringBuilder();

            footer.AppendLine("<div class='signature-section'>");
            footer.AppendLine("<div class='signature-box'>");
            footer.AppendLine("<div class='signature-line'>Customer Signature</div>");
            footer.AppendLine("</div>");
            footer.AppendLine("<div class='signature-box'>");
            footer.AppendLine("<div class='signature-line'>Authorized Signature</div>");
            footer.AppendLine("</div>");
            footer.AppendLine("</div>");

            if (!string.IsNullOrEmpty(settings.FooterText))
            {
                footer.AppendLine("<div style='text-align: center; margin-top: 20px; font-size: 10px; font-style: italic;'>");
                footer.AppendLine(settings.FooterText);
                footer.AppendLine("</div>");
            }

            return footer.ToString();
        }
    }
}
