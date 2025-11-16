import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

import {
  IPrintSettings,
  PrintReportType,
  PaperSize,
  PrintOrientation,
  FontSize,
} from '../interfaces/print.interfaces';

@Injectable({
  providedIn: 'root',
})
export class PrintConfigurationService {
  private configurationSubject = new BehaviorSubject<
    Map<PrintReportType, Partial<IPrintSettings>>
  >(new Map());
  private globalSettingsSubject = new BehaviorSubject<Partial<IPrintSettings>>(
    {}
  );

  public configuration$ = this.configurationSubject.asObservable();
  public globalSettings$ = this.globalSettingsSubject.asObservable();

  constructor() {
    this.initializeDefaultConfigurations();
  }

  /**
   * Initialize default configurations for different report types
   */
  private initializeDefaultConfigurations(): void {
    const configurations = new Map<PrintReportType, Partial<IPrintSettings>>();

    // Supplier Payment Configuration
    configurations.set(PrintReportType.SupplierPayment, {
      reportTitle: 'Payment Receipt',
      reportNumberPrefix: 'PR',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showSupplierInfo: true,
      showPaymentDetails: true,
      showAmountSummary: true,
      showSignature: true,
      fontSize: FontSize.Medium,
      defaultCopies: 1,
    });

    // Sales Invoice Configuration
    configurations.set(PrintReportType.SalesInvoice, {
      reportTitle: 'Sales Invoice',
      reportNumberPrefix: 'INV',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showAmountSummary: true,
      showTerms: true,
      fontSize: FontSize.Medium,
      defaultCopies: 2,
    });

    // Purchase Order Configuration
    configurations.set(PrintReportType.PurchaseOrder, {
      reportTitle: 'Purchase Order',
      reportNumberPrefix: 'PO',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showSupplierInfo: true,
      showTerms: true,
      fontSize: FontSize.Medium,
      defaultCopies: 2,
    });

    // Receipt Configuration (for thermal printers)
    configurations.set(PrintReportType.Receipt, {
      reportTitle: 'Receipt',
      reportNumberPrefix: 'RCP',
      paperSize: PaperSize.Thermal80mm,
      orientation: PrintOrientation.Portrait,
      fontSize: FontSize.Small,
      defaultCopies: 1,
      marginTop: 5,
      marginBottom: 5,
      marginLeft: 5,
      marginRight: 5,
    });

    // Invoice Configuration
    configurations.set(PrintReportType.Invoice, {
      reportTitle: 'Invoice',
      reportNumberPrefix: 'INV',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showAmountSummary: true,
      showTerms: true,
      showSignature: true,
      fontSize: FontSize.Medium,
      defaultCopies: 2,
    });

    // Statement Configuration
    configurations.set(PrintReportType.Statement, {
      reportTitle: 'Statement',
      reportNumberPrefix: 'STM',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showAmountSummary: true,
      fontSize: FontSize.Medium,
      defaultCopies: 1,
    });

    // Voucher Configuration
    configurations.set(PrintReportType.Voucher, {
      reportTitle: 'Voucher',
      reportNumberPrefix: 'VCH',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showSignature: true,
      fontSize: FontSize.Medium,
      defaultCopies: 2,
    });

    // Delivery Note Configuration
    configurations.set(PrintReportType.DeliveryNote, {
      reportTitle: 'Delivery Note',
      reportNumberPrefix: 'DN',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showSignature: true,
      fontSize: FontSize.Medium,
      defaultCopies: 2,
    });

    // Credit/Debit Note Configuration
    configurations.set(PrintReportType.CreditNote, {
      reportTitle: 'Credit Note',
      reportNumberPrefix: 'CN',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showAmountSummary: true,
      fontSize: FontSize.Medium,
      defaultCopies: 1,
    });

    configurations.set(PrintReportType.DebitNote, {
      reportTitle: 'Debit Note',
      reportNumberPrefix: 'DN',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      showAmountSummary: true,
      fontSize: FontSize.Medium,
      defaultCopies: 1,
    });

    // Report Configuration
    configurations.set(PrintReportType.Report, {
      reportTitle: 'Report',
      reportNumberPrefix: 'RPT',
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      fontSize: FontSize.Medium,
      defaultCopies: 1,
    });

    this.configurationSubject.next(configurations);
  }

  /**
   * Get configuration for a specific report type
   */
  getReportConfiguration(reportType: PrintReportType): Partial<IPrintSettings> {
    const configurations = this.configurationSubject.value;
    return configurations.get(reportType) || {};
  }

  /**
   * Update configuration for a specific report type
   */
  updateReportConfiguration(
    reportType: PrintReportType,
    settings: Partial<IPrintSettings>
  ): void {
    const configurations = this.configurationSubject.value;
    const existingConfig = configurations.get(reportType) || {};
    const updatedConfig = { ...existingConfig, ...settings };

    configurations.set(reportType, updatedConfig);
    this.configurationSubject.next(new Map(configurations));
  }

  /**
   * Get global settings that apply to all reports
   */
  getGlobalSettings(): Partial<IPrintSettings> {
    return this.globalSettingsSubject.value;
  }

  /**
   * Update global settings
   */
  updateGlobalSettings(settings: Partial<IPrintSettings>): void {
    const currentSettings = this.globalSettingsSubject.value;
    const updatedSettings = { ...currentSettings, ...settings };
    this.globalSettingsSubject.next(updatedSettings);
  }

  /**
   * Get merged settings for a report type (global + report-specific)
   */
  getMergedSettings(
    reportType: PrintReportType,
    branchSettings?: Partial<IPrintSettings>
  ): Partial<IPrintSettings> {
    const globalSettings = this.getGlobalSettings();
    const reportSettings = this.getReportConfiguration(reportType);

    return {
      ...globalSettings,
      ...reportSettings,
      ...branchSettings,
    };
  }

  /**
   * Reset configuration for a report type to defaults
   */
  resetReportConfiguration(reportType: PrintReportType): void {
    const configurations = this.configurationSubject.value;
    configurations.delete(reportType);
    this.configurationSubject.next(new Map(configurations));

    // Reinitialize with defaults
    this.initializeDefaultConfigurations();
  }

  /**
   * Get all available paper sizes
   */
  getAvailablePaperSizes(): {
    value: PaperSize;
    label: string;
    description: string;
  }[] {
    return [
      { value: PaperSize.A4, label: 'A4', description: '210 × 297 mm' },
      { value: PaperSize.A5, label: 'A5', description: '148 × 210 mm' },
      {
        value: PaperSize.Letter,
        label: 'Letter',
        description: '8.5 × 11 inches',
      },
      {
        value: PaperSize.Legal,
        label: 'Legal',
        description: '8.5 × 14 inches',
      },
      {
        value: PaperSize.Thermal80mm,
        label: 'Thermal 80mm',
        description: '80mm thermal receipt',
      },
      {
        value: PaperSize.Thermal58mm,
        label: 'Thermal 58mm',
        description: '58mm thermal receipt',
      },
      { value: PaperSize.Custom, label: 'Custom', description: 'Custom size' },
    ];
  }

  /**
   * Get all available orientations
   */
  getAvailableOrientations(): { value: PrintOrientation; label: string }[] {
    return [
      { value: PrintOrientation.Portrait, label: 'Portrait' },
      { value: PrintOrientation.Landscape, label: 'Landscape' },
    ];
  }

  /**
   * Get all available font sizes
   */
  getAvailableFontSizes(): {
    value: FontSize;
    label: string;
    description: string;
  }[] {
    return [
      { value: FontSize.Small, label: 'Small', description: '12px' },
      { value: FontSize.Medium, label: 'Medium', description: '14px' },
      { value: FontSize.Large, label: 'Large', description: '16px' },
      { value: FontSize.ExtraLarge, label: 'Extra Large', description: '18px' },
    ];
  }

  /**
   * Validate settings for a report type
   */
  validateSettings(
    reportType: PrintReportType,
    settings: Partial<IPrintSettings>
  ): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];

    // Check thermal paper compatibility
    if (
      settings.paperSize === PaperSize.Thermal80mm ||
      settings.paperSize === PaperSize.Thermal58mm
    ) {
      if (settings.orientation === PrintOrientation.Landscape) {
        errors.push('Thermal printers only support portrait orientation');
      }
      if (
        settings.fontSize === FontSize.Large ||
        settings.fontSize === FontSize.ExtraLarge
      ) {
        errors.push(
          'Large font sizes are not recommended for thermal receipts'
        );
      }
    }

    // Check margin settings
    if (settings.marginTop !== undefined && settings.marginTop < 0) {
      errors.push('Top margin cannot be negative');
    }
    if (settings.marginBottom !== undefined && settings.marginBottom < 0) {
      errors.push('Bottom margin cannot be negative');
    }
    if (settings.marginLeft !== undefined && settings.marginLeft < 0) {
      errors.push('Left margin cannot be negative');
    }
    if (settings.marginRight !== undefined && settings.marginRight < 0) {
      errors.push('Right margin cannot be negative');
    }

    // Check copies
    if (
      settings.defaultCopies !== undefined &&
      (settings.defaultCopies < 1 || settings.defaultCopies > 10)
    ) {
      errors.push('Number of copies must be between 1 and 10');
    }

    return {
      isValid: errors.length === 0,
      errors,
    };
  }

  /**
   * Get recommended settings for a report type based on usage
   */
  getRecommendedSettings(reportType: PrintReportType): Partial<IPrintSettings> {
    const recommendations: {
      [key in PrintReportType]?: Partial<IPrintSettings>;
    } = {
      [PrintReportType.Receipt]: {
        paperSize: PaperSize.Thermal80mm,
        fontSize: FontSize.Small,
        marginTop: 5,
        marginBottom: 5,
        showLogo: false, // Saves space
      },
      [PrintReportType.Invoice]: {
        paperSize: PaperSize.A4,
        fontSize: FontSize.Medium,
        showTerms: true,
        showSignature: true,
        defaultCopies: 2,
      },
      [PrintReportType.Report]: {
        paperSize: PaperSize.A4,
        orientation: PrintOrientation.Landscape, // More space for data
        fontSize: FontSize.Small,
      },
    };

    return recommendations[reportType] || {};
  }

  /**
   * Export configuration as JSON
   */
  exportConfiguration(): string {
    const config = {
      globalSettings: this.getGlobalSettings(),
      reportConfigurations: Object.fromEntries(this.configurationSubject.value),
    };
    return JSON.stringify(config, null, 2);
  }

  /**
   * Import configuration from JSON
   */
  importConfiguration(configJson: string): {
    success: boolean;
    error?: string;
  } {
    try {
      const config = JSON.parse(configJson);

      if (config.globalSettings) {
        this.updateGlobalSettings(config.globalSettings);
      }

      if (config.reportConfigurations) {
        const configurations = new Map<
          PrintReportType,
          Partial<IPrintSettings>
        >();
        Object.entries(config.reportConfigurations).forEach(([key, value]) => {
          configurations.set(
            key as PrintReportType,
            value as Partial<IPrintSettings>
          );
        });
        this.configurationSubject.next(configurations);
      }

      return { success: true };
    } catch (error) {
      return {
        success: false,
        error:
          error instanceof Error
            ? error.message
            : 'Invalid configuration format',
      };
    }
  }
}
