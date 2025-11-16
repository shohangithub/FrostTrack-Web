import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  IPrintable,
  IPrintResult,
  IPrintPreviewData,
  PrintReportType,
  PrintFormat,
} from '../interfaces/print.interfaces';
import { PrintService } from '../services/print.service';
import { PrintTemplateService } from '../services/print-template.service';
import { PrintConfigurationService } from '../services/print-configuration.service';

/**
 * Helper service that provides simple methods for common print operations
 */
@Injectable({
  providedIn: 'root',
})
export class PrintHelperService {
  constructor(
    private printService: PrintService,
    private templateService: PrintTemplateService,
    private configService: PrintConfigurationService
  ) {}

  /**
   * Quick print for supplier payments
   */
  async printSupplierPayment(
    paymentData: any,
    branchId: number = 1
  ): Promise<IPrintResult> {
    const printable: IPrintable = {
      id: `supplier-payment-${paymentData.id || Date.now()}`,
      type: PrintReportType.SupplierPayment,
      data: {
        ...paymentData,
        branchId,
      },
    };

    return this.printService.printReport(printable, PrintFormat.HTML);
  }

  /**
   * Quick print for sales invoices
   */
  async printSalesInvoice(
    invoiceData: any,
    branchId: number = 1
  ): Promise<IPrintResult> {
    const printable: IPrintable = {
      id: `sales-invoice-${invoiceData.id || Date.now()}`,
      type: PrintReportType.SalesInvoice,
      data: {
        ...invoiceData,
        branchId,
      },
    };

    return this.printService.printReport(printable, PrintFormat.HTML);
  }

  /**
   * Quick print for purchase orders
   */
  async printPurchaseOrder(
    orderData: any,
    branchId: number = 1
  ): Promise<IPrintResult> {
    const printable: IPrintable = {
      id: `purchase-order-${orderData.id || Date.now()}`,
      type: PrintReportType.PurchaseOrder,
      data: {
        ...orderData,
        branchId,
      },
    };

    return this.printService.printReport(printable, PrintFormat.HTML);
  }

  /**
   * Quick print for receipts (thermal printer friendly)
   */
  async printReceipt(
    receiptData: any,
    branchId: number = 1
  ): Promise<IPrintResult> {
    const printable: IPrintable = {
      id: `receipt-${receiptData.id || Date.now()}`,
      type: PrintReportType.Receipt,
      data: {
        ...receiptData,
        branchId,
      },
      customSettings: {
        paperSize: 'Thermal80mm' as any,
        fontSize: 'small' as any,
        marginTop: 5,
        marginBottom: 5,
        marginLeft: 5,
        marginRight: 5,
      },
    };

    return this.printService.printReport(printable, PrintFormat.HTML);
  }

  /**
   * Generate PDF for any report type
   */
  async generatePDF(
    reportData: any,
    reportType: PrintReportType,
    branchId: number = 1
  ): Promise<IPrintResult> {
    const printable: IPrintable = {
      id: `pdf-${reportType}-${Date.now()}`,
      type: reportType,
      data: {
        ...reportData,
        branchId,
      },
    };

    return this.printService.printReport(printable, PrintFormat.PDF);
  }

  /**
   * Show preview for any report
   */
  async showPreview(
    reportData: any,
    reportType: PrintReportType,
    branchId: number = 1
  ): Promise<IPrintPreviewData> {
    const printable: IPrintable = {
      id: `preview-${reportType}-${Date.now()}`,
      type: reportType,
      data: {
        ...reportData,
        branchId,
      },
    };

    return this.printService.generatePreview(printable);
  }

  /**
   * Get available templates for a report type
   */
  getAvailableTemplates(reportType: PrintReportType) {
    return this.templateService.getTemplatesForType(reportType);
  }

  /**
   * Get print settings for a branch
   */
  getPrintSettings(branchId: number) {
    return this.printService.getPrintSettings(branchId);
  }

  /**
   * Update print settings for a branch
   */
  updatePrintSettings(settings: any) {
    return this.printService.updatePrintSettings(settings);
  }

  /**
   * Get recommended settings for a report type
   */
  getRecommendedSettings(reportType: PrintReportType) {
    return this.configService.getRecommendedSettings(reportType);
  }

  /**
   * Validate print data for a report type
   */
  validatePrintData(data: any, reportType: PrintReportType) {
    return this.templateService.validateTemplateData(data, reportType);
  }

  /**
   * Bulk print multiple documents
   */
  async bulkPrint(
    printRequests: Array<{
      data: any;
      type: PrintReportType;
      format?: PrintFormat;
    }>,
    branchId: number = 1
  ): Promise<IPrintResult[]> {
    const results: IPrintResult[] = [];

    for (const request of printRequests) {
      const printable: IPrintable = {
        id: `bulk-${request.type}-${Date.now()}-${Math.random()}`,
        type: request.type,
        data: {
          ...request.data,
          branchId,
        },
      };

      try {
        const result = await this.printService.printReport(
          printable,
          request.format || PrintFormat.HTML
        );
        results.push(result);
      } catch (error) {
        results.push({
          success: false,
          message: 'Bulk print failed',
          error: error instanceof Error ? error.message : 'Unknown error',
        });
      }
    }

    return results;
  }

  /**
   * Get print job status
   */
  getPrintJobStatus(): Observable<string> {
    return this.printService.printJob$;
  }

  /**
   * Clear print cache
   */
  clearPrintCache(): void {
    this.printService.clearCache();
  }

  /**
   * Export print configuration
   */
  exportConfiguration(): string {
    return this.configService.exportConfiguration();
  }

  /**
   * Import print configuration
   */
  importConfiguration(configJson: string) {
    return this.configService.importConfiguration(configJson);
  }
}
