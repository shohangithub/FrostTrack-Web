import {
  Directive,
  Input,
  Output,
  EventEmitter,
  HostListener,
} from '@angular/core';

import {
  IPrintable,
  IPrintResult,
  PrintFormat,
  PrintReportType,
} from '../interfaces/print.interfaces';
import { PrintService } from '../services/print.service';

@Directive({
  selector: '[appPrint]',
  standalone: true,
})
export class PrintDirective {
  @Input('appPrint') printData: any;
  @Input() printType: PrintReportType = PrintReportType.Report;
  @Input() printFormat: PrintFormat = PrintFormat.HTML;
  @Input() printTemplate?: string;
  @Input() printSettings?: any;
  @Input() showPreview = true;

  @Output() printStarted = new EventEmitter<void>();
  @Output() printCompleted = new EventEmitter<IPrintResult>();
  @Output() printFailed = new EventEmitter<string>();

  constructor(private printService: PrintService) {}

  @HostListener('click', ['$event'])
  async onClick(event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();

    if (!this.printData) {
      this.printFailed.emit('No print data provided');
      return;
    }

    try {
      this.printStarted.emit();

      const printable: IPrintable = {
        id: this.generatePrintId(),
        type: this.printType,
        data: this.printData,
        templateName: this.printTemplate,
        customSettings: this.printSettings,
      };

      if (this.showPreview) {
        // Generate preview data
        const previewData = await this.printService.generatePreview(printable);
        // Note: In a real implementation, you'd show a preview modal here
        // For now, we'll proceed directly to print
        console.log('Preview data generated:', previewData);
      }

      const result = await this.printService.printReport(
        printable,
        this.printFormat
      );

      if (result.success) {
        this.printCompleted.emit(result);
      } else {
        this.printFailed.emit(result.message || 'Print failed');
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : 'Unknown print error';
      this.printFailed.emit(errorMessage);
    }
  }

  private generatePrintId(): string {
    return `print_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}
