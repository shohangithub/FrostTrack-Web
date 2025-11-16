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

        public async Task<PaymentReceiptData> GetPaymentReceiptDataAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            var payment = await _printSettingsRepository.GetSupplierPaymentByIdAsync(paymentId, cancellationToken);

            if (payment == null)
                throw new ArgumentException($"Payment with ID {paymentId} not found.");

            // Calculate due amounts (simplified - you might want to implement proper calculation)
            var previousDue = payment.Supplier?.OpeningBalance ?? 0;
            var remainingDue = Math.Max(0, previousDue - payment.PaymentAmount);

            return new PaymentReceiptData
            {
                Id = (int)payment.Id,
                PaymentNumber = payment.PaymentNumber,
                PaymentDate = payment.PaymentDate,
                PaymentType = payment.PaymentType,
                Supplier = new SupplierInfo
                {
                    Name = payment.Supplier?.SupplierName ?? "Unknown Supplier",
                    Phone = payment.Supplier?.SupplierMobile ?? payment.Supplier?.OfficePhone ?? "",
                    Address = payment.Supplier?.Address ?? "",
                    Email = payment.Supplier?.SupplierEmail ?? ""
                },
                PaymentMethod = payment.PaymentMethod,
                PaymentAmount = payment.PaymentAmount,
                PreviousDue = previousDue,
                RemainingDue = remainingDue,
                Notes = payment.Notes ?? "",
                CreatedBy = "System", // You might want to get this from the audit fields
                BranchName = payment.Branch?.Name ?? "Main Branch",
                PrintDateTime = DateTime.Now
            };
        }

        public async Task<string> GeneratePaymentReceiptHtmlAsync(PaymentReceiptData paymentData, PrintSettingsResponse printSettings, CancellationToken cancellationToken = default)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Payment Receipt</title>");
            html.AppendLine(GetPrintStyles(printSettings));
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class='print-container'>");

            // Header
            html.AppendLine(GenerateHeader(printSettings, paymentData));

            // Content
            html.AppendLine("<div class='print-content'>");

            // Receipt Info
            html.AppendLine(GenerateReceiptInfo(paymentData, printSettings));

            // Supplier Info
            if (printSettings.ShowSupplierInfo)
            {
                html.AppendLine(GenerateSupplierInfo(paymentData));
            }

            // Payment Details
            if (printSettings.ShowPaymentDetails)
            {
                html.AppendLine(GeneratePaymentDetails(paymentData));
            }

            // Amount Summary
            if (printSettings.ShowAmountSummary)
            {
                html.AppendLine(GenerateAmountSummary(paymentData));
            }

            // Notes
            if (printSettings.ShowNotes && !string.IsNullOrEmpty(paymentData.Notes))
            {
                html.AppendLine(GenerateNotes(paymentData));
            }

            html.AppendLine("</div>");

            // Footer
            html.AppendLine(GenerateFooter(printSettings, paymentData));

            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        public async Task<byte[]> GeneratePaymentReceiptPdfAsync(PaymentReceiptData paymentData, PrintSettingsResponse printSettings, CancellationToken cancellationToken = default)
        {
            // For PDF generation, you would typically use a library like iTextSharp, PuppeteerSharp, or similar
            // For now, this is a placeholder that would need implementation based on your PDF library of choice
            var html = await GeneratePaymentReceiptHtmlAsync(paymentData, printSettings, cancellationToken);

            // Placeholder - implement with your preferred PDF library
            // Example with PuppeteerSharp:
            // using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            // using var page = await browser.NewPageAsync();
            // await page.SetContentAsync(html);
            // var pdf = await page.PdfAsync(new PdfOptions { Format = PaperFormat.A4 });
            // return pdf;

            throw new NotImplementedException("PDF generation not implemented. Implement with your preferred PDF library.");
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

        private string GenerateHeader(PrintSettingsResponse settings, PaymentReceiptData paymentData)
        {
            var header = new StringBuilder();
            header.AppendLine("<div class='print-header'>");

            if (settings.ShowLogo && !string.IsNullOrEmpty(settings.LogoUrl))
            {
                header.AppendLine($"<div class='company-logo mb-2'>");
                header.AppendLine($"<img src='{settings.LogoUrl}' alt='Company Logo' style='max-height: 60px;' />");
                header.AppendLine("</div>");
            }

            header.AppendLine($"<h1>{settings.CompanyName}</h1>");

            header.AppendLine("<div class='company-info'>");
            if (!string.IsNullOrEmpty(settings.CompanyAddress))
                header.AppendLine($"<div>{settings.CompanyAddress}</div>");
            if (!string.IsNullOrEmpty(settings.CompanyPhone))
                header.AppendLine($"<div>Phone: {settings.CompanyPhone}</div>");
            if (!string.IsNullOrEmpty(settings.CompanyEmail))
                header.AppendLine($"<div>Email: {settings.CompanyEmail}</div>");
            if (!string.IsNullOrEmpty(settings.CompanyWebsite))
                header.AppendLine($"<div>Web: {settings.CompanyWebsite}</div>");
            header.AppendLine("</div>");

            if (settings.ShowBranchInfo && !string.IsNullOrEmpty(settings.BranchName))
            {
                header.AppendLine("<hr style='margin: 10px 0; border: 1px solid #ccc;'>");
                header.AppendLine($"<div><strong>Branch: {settings.BranchName}</strong></div>");
                if (!string.IsNullOrEmpty(settings.BranchAddress))
                    header.AppendLine($"<div>{settings.BranchAddress}</div>");
                if (!string.IsNullOrEmpty(settings.BranchPhone))
                    header.AppendLine($"<div>Phone: {settings.BranchPhone}</div>");
            }

            header.AppendLine($"<h2 style='margin: 15px 0 10px 0; font-size: 18px;'>{settings.PaymentReceiptTitle}</h2>");
            header.AppendLine("</div>");

            return header.ToString();
        }

        private string GenerateReceiptInfo(PaymentReceiptData paymentData, PrintSettingsResponse settings)
        {
            var info = new StringBuilder();
            info.AppendLine("<div class='receipt-info mb-3'>");
            info.AppendLine("<div class='row'>");
            info.AppendLine("<span class='col-label'>Receipt No:</span>");
            info.AppendLine($"<span class='col-value'>{settings.ReceiptNumberPrefix}{paymentData.PaymentNumber}</span>");
            info.AppendLine("</div>");
            info.AppendLine("<div class='row'>");
            info.AppendLine("<span class='col-label'>Payment Date:</span>");
            info.AppendLine($"<span class='col-value'>{paymentData.PaymentDate:dd/MM/yyyy}</span>");
            info.AppendLine("</div>");
            info.AppendLine("<div class='row'>");
            info.AppendLine("<span class='col-label'>Print Date:</span>");
            info.AppendLine($"<span class='col-value'>{paymentData.PrintDateTime:dd/MM/yyyy HH:mm}</span>");
            info.AppendLine("</div>");
            info.AppendLine("<div class='row'>");
            info.AppendLine("<span class='col-label'>Payment Type:</span>");
            info.AppendLine($"<span class='col-value'>{paymentData.PaymentType}</span>");
            info.AppendLine("</div>");
            info.AppendLine("</div>");
            return info.ToString();
        }

        private string GenerateSupplierInfo(PaymentReceiptData paymentData)
        {
            var info = new StringBuilder();
            info.AppendLine("<div class='supplier-info mb-3'>");
            info.AppendLine("<h3 style='margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;'>Supplier Information</h3>");
            info.AppendLine("<div class='row'>");
            info.AppendLine("<span class='col-label'>Supplier Name:</span>");
            info.AppendLine($"<span class='col-value'>{paymentData.Supplier.Name}</span>");
            info.AppendLine("</div>");

            if (!string.IsNullOrEmpty(paymentData.Supplier.Phone))
            {
                info.AppendLine("<div class='row'>");
                info.AppendLine("<span class='col-label'>Phone:</span>");
                info.AppendLine($"<span class='col-value'>{paymentData.Supplier.Phone}</span>");
                info.AppendLine("</div>");
            }

            if (!string.IsNullOrEmpty(paymentData.Supplier.Address))
            {
                info.AppendLine("<div class='row'>");
                info.AppendLine("<span class='col-label'>Address:</span>");
                info.AppendLine($"<span class='col-value'>{paymentData.Supplier.Address}</span>");
                info.AppendLine("</div>");
            }

            if (!string.IsNullOrEmpty(paymentData.Supplier.Email))
            {
                info.AppendLine("<div class='row'>");
                info.AppendLine("<span class='col-label'>Email:</span>");
                info.AppendLine($"<span class='col-value'>{paymentData.Supplier.Email}</span>");
                info.AppendLine("</div>");
            }

            info.AppendLine("</div>");
            return info.ToString();
        }

        private string GeneratePaymentDetails(PaymentReceiptData paymentData)
        {
            var details = new StringBuilder();
            details.AppendLine("<div class='payment-details mb-3'>");
            details.AppendLine("<h3 style='margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;'>Payment Details</h3>");
            details.AppendLine("<table class='receipt-table'>");
            details.AppendLine("<tr>");
            details.AppendLine("<td><strong>Payment Method:</strong></td>");
            details.AppendLine($"<td>{paymentData.PaymentMethod}</td>");
            details.AppendLine("</tr>");

            if (paymentData.PaymentDetails != null)
            {
                if (!string.IsNullOrEmpty(paymentData.PaymentDetails.BankName))
                {
                    details.AppendLine("<tr>");
                    details.AppendLine("<td><strong>Bank Name:</strong></td>");
                    details.AppendLine($"<td>{paymentData.PaymentDetails.BankName}</td>");
                    details.AppendLine("</tr>");
                }

                if (!string.IsNullOrEmpty(paymentData.PaymentDetails.CheckNumber))
                {
                    details.AppendLine("<tr>");
                    details.AppendLine("<td><strong>Check Number:</strong></td>");
                    details.AppendLine($"<td>{paymentData.PaymentDetails.CheckNumber}</td>");
                    details.AppendLine("</tr>");
                }

                if (paymentData.PaymentDetails.CheckDate.HasValue)
                {
                    details.AppendLine("<tr>");
                    details.AppendLine("<td><strong>Check Date:</strong></td>");
                    details.AppendLine($"<td>{paymentData.PaymentDetails.CheckDate.Value:dd/MM/yyyy}</td>");
                    details.AppendLine("</tr>");
                }
            }

            details.AppendLine("</table>");
            details.AppendLine("</div>");
            return details.ToString();
        }

        private string GenerateAmountSummary(PaymentReceiptData paymentData)
        {
            var summary = new StringBuilder();
            summary.AppendLine("<div class='amount-summary mb-3'>");
            summary.AppendLine("<h3 style='margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;'>Amount Summary</h3>");
            summary.AppendLine("<table class='receipt-table'>");
            summary.AppendLine("<tr>");
            summary.AppendLine("<td><strong>Previous Due Amount:</strong></td>");
            summary.AppendLine($"<td class='text-right'>${paymentData.PreviousDue:F2}</td>");
            summary.AppendLine("</tr>");
            summary.AppendLine("<tr class='amount-row'>");
            summary.AppendLine("<td><strong>Payment Amount:</strong></td>");
            summary.AppendLine($"<td class='text-right'><strong>${paymentData.PaymentAmount:F2}</strong></td>");
            summary.AppendLine("</tr>");
            summary.AppendLine("<tr>");
            summary.AppendLine("<td><strong>Remaining Due:</strong></td>");
            summary.AppendLine($"<td class='text-right'>${paymentData.RemainingDue:F2}</td>");
            summary.AppendLine("</tr>");
            summary.AppendLine("</table>");
            summary.AppendLine("</div>");
            return summary.ToString();
        }

        private string GenerateNotes(PaymentReceiptData paymentData)
        {
            var notes = new StringBuilder();
            notes.AppendLine("<div class='notes mb-3'>");
            notes.AppendLine("<h3 style='margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;'>Notes</h3>");
            notes.AppendLine($"<p style='margin: 0; padding: 10px; background: #f8f9fa; border-left: 4px solid #007bff;'>");
            notes.AppendLine(paymentData.Notes);
            notes.AppendLine("</p>");
            notes.AppendLine("</div>");
            return notes.ToString();
        }

        private string GenerateFooter(PrintSettingsResponse settings, PaymentReceiptData paymentData)
        {
            var footer = new StringBuilder();
            footer.AppendLine("<div class='print-footer'>");

            if (!string.IsNullOrEmpty(settings.ThankYouMessage))
            {
                footer.AppendLine("<div class='mb-2'>");
                footer.AppendLine($"<strong>{settings.ThankYouMessage}</strong>");
                footer.AppendLine("</div>");
            }

            if (!string.IsNullOrEmpty(settings.FooterText))
            {
                footer.AppendLine("<div class='mb-2'>");
                footer.AppendLine(settings.FooterText);
                footer.AppendLine("</div>");
            }

            if (!string.IsNullOrEmpty(settings.TermsAndConditions))
            {
                footer.AppendLine("<div class='mb-2' style='font-size: 10px;'>");
                footer.AppendLine($"<em>{settings.TermsAndConditions}</em>");
                footer.AppendLine("</div>");
            }

            if (!string.IsNullOrEmpty(settings.AuthorizedBy) || !string.IsNullOrEmpty(settings.Signature))
            {
                footer.AppendLine("<div class='signature-section mt-3'>");
                footer.AppendLine("<div style='border-top: 1px solid #333; width: 200px; margin: 20px auto 5px auto;'></div>");
                if (!string.IsNullOrEmpty(settings.AuthorizedBy))
                    footer.AppendLine($"<div>Authorized by: {settings.AuthorizedBy}</div>");
                if (!string.IsNullOrEmpty(settings.Signature))
                    footer.AppendLine($"<div>{settings.Signature}</div>");
                footer.AppendLine("</div>");
            }

            footer.AppendLine("<div style='margin-top: 20px;'>");
            footer.AppendLine($"<div>Processed by: {paymentData.CreatedBy}</div>");
            footer.AppendLine($"<div>Branch: {paymentData.BranchName}</div>");
            footer.AppendLine("</div>");

            footer.AppendLine("</div>");
            return footer.ToString();
        }
    }
}