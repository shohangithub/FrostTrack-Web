import {
  Component,
  Input,
  OnInit,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';

declare const JsBarcode: any;

/**
 * Reusable Barcode Component
 *
 * Usage Examples:
 *
 * Basic usage:
 * <app-barcode [value]="'123456789'" [showText]="true"></app-barcode>
 *
 * With custom settings:
 * <app-barcode
 *   [value]="bookingNumber"
 *   [format]="'CODE128'"
 *   [width]="2"
 *   [height]="50"
 *   [displayValue]="false"
 *   [showText]="true"
 * ></app-barcode>
 *
 * Supported formats: CODE128, EAN13, UPC, CODE39, ITF14, MSI, Pharmacode, Codabar
 */
@Component({
  selector: 'app-barcode',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="barcode-container" *ngIf="value">
      <svg [id]="'barcode-' + elementId" class="barcode"></svg>
      <div class="barcode-text" *ngIf="showText">{{ value }}</div>
    </div>
  `,
  styles: [
    `
      .barcode-container {
        display: inline-block;
        text-align: center;
      }

      .barcode {
        max-width: 100%;
        height: auto;
      }

      .barcode-text {
        font-size: 12px;
        font-weight: bold;
        margin-top: 5px;
      }
    `,
  ],
})
export class BarcodeComponent implements OnInit, OnChanges {
  @Input() value: string = '';
  @Input() format: string = 'CODE128';
  @Input() width: number = 2;
  @Input() height: number = 50;
  @Input() displayValue: boolean = false;
  @Input() showText: boolean = false;
  @Input() elementId: string = '';

  private scriptLoaded = false;

  ngOnInit(): void {
    if (!this.elementId) {
      this.elementId = this.generateRandomId();
    }
    this.loadAndGenerateBarcode();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['value'] && !changes['value'].firstChange) {
      this.loadAndGenerateBarcode();
    }
  }

  private generateRandomId(): string {
    return 'barcode-' + Math.random().toString(36).substr(2, 9);
  }

  private loadAndGenerateBarcode(): void {
    if (typeof JsBarcode === 'undefined' && !this.scriptLoaded) {
      this.scriptLoaded = true;
      const script = document.createElement('script');
      script.src =
        'https://cdn.jsdelivr.net/npm/jsbarcode@3.11.5/dist/JsBarcode.all.min.js';
      script.onload = () => {
        setTimeout(() => this.renderBarcode(), 100);
      };
      document.head.appendChild(script);
    } else if (typeof JsBarcode !== 'undefined') {
      setTimeout(() => this.renderBarcode(), 100);
    }
  }

  private renderBarcode(): void {
    const barcodeElement = document.getElementById('barcode-' + this.elementId);
    if (barcodeElement && this.value && typeof JsBarcode !== 'undefined') {
      try {
        JsBarcode(barcodeElement, this.value, {
          format: this.format,
          width: this.width,
          height: this.height,
          displayValue: this.displayValue,
        });
      } catch (error) {
        console.error('Failed to generate barcode:', error);
      }
    }
  }
}
