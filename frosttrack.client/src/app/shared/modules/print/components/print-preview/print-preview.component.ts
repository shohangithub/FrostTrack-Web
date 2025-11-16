import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';

import {
  IPrintPreviewData,
  IPrintSettings,
  PrintFormat,
  IPrintResult,
} from '../../interfaces/print.interfaces';
import { PrintService } from '../../services/print.service';

@Component({
  selector: 'app-print-preview',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="print-preview-modal" *ngIf="visible">
      <div class="modal-backdrop" (click)="onClose()"></div>
      <div class="modal-content">
        <div class="modal-header">
          <h3>{{ previewData?.title || 'Print Preview' }}</h3>
          <button type="button" class="btn-close" (click)="onClose()">
            &times;
          </button>
        </div>

        <div class="modal-body">
          <!-- Print Options -->
          <div class="print-options">
            <form [formGroup]="printOptionsForm" class="options-form">
              <div class="form-row">
                <div class="form-group">
                  <label for="format">Format:</label>
                  <select
                    id="format"
                    formControlName="format"
                    class="form-control"
                  >
                    <option value="html">HTML (Print)</option>
                    <option value="pdf">PDF (Download)</option>
                    <option value="image">Image (Download)</option>
                  </select>
                </div>

                <div class="form-group">
                  <label for="copies">Copies:</label>
                  <input
                    type="number"
                    id="copies"
                    formControlName="copies"
                    class="form-control"
                    min="1"
                    max="10"
                  />
                </div>

                <div class="form-group" *ngIf="showCustomSettings">
                  <label for="paperSize">Paper Size:</label>
                  <select
                    id="paperSize"
                    formControlName="paperSize"
                    class="form-control"
                  >
                    <option value="A4">A4</option>
                    <option value="A5">A5</option>
                    <option value="Letter">Letter</option>
                    <option value="Legal">Legal</option>
                    <option value="Thermal80mm">Thermal 80mm</option>
                    <option value="Thermal58mm">Thermal 58mm</option>
                  </select>
                </div>

                <div class="form-group" *ngIf="showCustomSettings">
                  <label for="orientation">Orientation:</label>
                  <select
                    id="orientation"
                    formControlName="orientation"
                    class="form-control"
                  >
                    <option value="portrait">Portrait</option>
                    <option value="landscape">Landscape</option>
                  </select>
                </div>
              </div>

              <div class="form-row" *ngIf="showCustomSettings">
                <div class="form-group">
                  <label>
                    <input type="checkbox" formControlName="showLogo" />
                    Show Logo
                  </label>
                </div>

                <div class="form-group">
                  <label>
                    <input type="checkbox" formControlName="showSignature" />
                    Show Signature
                  </label>
                </div>

                <div class="form-group">
                  <label>
                    <input type="checkbox" formControlName="showTerms" />
                    Show Terms & Conditions
                  </label>
                </div>
              </div>
            </form>

            <div class="options-toggle">
              <button
                type="button"
                class="btn btn-link"
                (click)="showCustomSettings = !showCustomSettings"
              >
                {{ showCustomSettings ? 'Hide' : 'Show' }} Advanced Options
              </button>
            </div>
          </div>

          <!-- Print Status -->
          <div class="print-status" *ngIf="printStatus">
            <div
              class="alert"
              [ngClass]="
                'alert-' + (printStatus.success ? 'success' : 'danger')
              "
            >
              {{ printStatus.message }}
            </div>
          </div>

          <!-- Preview Content -->
          <div class="preview-container">
            <div class="preview-toolbar">
              <div class="zoom-controls">
                <button type="button" class="btn btn-sm" (click)="zoomOut()">
                  -
                </button>
                <span class="zoom-level">{{ zoomLevel }}%</span>
                <button type="button" class="btn btn-sm" (click)="zoomIn()">
                  +
                </button>
              </div>

              <button
                type="button"
                class="btn btn-sm btn-secondary"
                (click)="refreshPreview()"
              >
                Refresh Preview
              </button>
            </div>

            <div
              class="preview-content"
              [style.transform]="'scale(' + zoomLevel / 100 + ')'"
            >
              <div
                class="preview-frame"
                [innerHTML]="sanitizedHtml"
                [ngClass]="getPreviewClasses()"
              ></div>
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" (click)="onClose()">
            Cancel
          </button>

          <button
            type="button"
            class="btn btn-primary"
            (click)="onPrint()"
            [disabled]="isPrinting"
          >
            <span *ngIf="isPrinting" class="spinner"></span>
            {{ isPrinting ? 'Processing...' : 'Print' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .print-preview-modal {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        z-index: 1050;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .modal-backdrop {
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-color: rgba(0, 0, 0, 0.5);
      }

      .modal-content {
        position: relative;
        background: white;
        border-radius: 8px;
        width: 90vw;
        max-width: 1200px;
        height: 90vh;
        display: flex;
        flex-direction: column;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
      }

      .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem;
        border-bottom: 1px solid #dee2e6;
      }

      .modal-header h3 {
        margin: 0;
        color: #495057;
      }

      .btn-close {
        background: none;
        border: none;
        font-size: 1.5rem;
        cursor: pointer;
        color: #6c757d;
      }

      .modal-body {
        flex: 1;
        padding: 1rem;
        display: flex;
        flex-direction: column;
        overflow: hidden;
      }

      .print-options {
        margin-bottom: 1rem;
        padding: 1rem;
        background-color: #f8f9fa;
        border-radius: 4px;
      }

      .options-form {
        margin-bottom: 0.5rem;
      }

      .form-row {
        display: flex;
        gap: 1rem;
        align-items: end;
        margin-bottom: 0.5rem;
      }

      .form-group {
        display: flex;
        flex-direction: column;
        min-width: 120px;
      }

      .form-group label {
        font-size: 0.875rem;
        font-weight: 500;
        margin-bottom: 0.25rem;
        color: #495057;
      }

      .form-control {
        padding: 0.375rem 0.75rem;
        border: 1px solid #ced4da;
        border-radius: 4px;
        font-size: 0.875rem;
      }

      .options-toggle {
        text-align: center;
      }

      .btn-link {
        background: none;
        border: none;
        color: #007bff;
        text-decoration: underline;
        cursor: pointer;
        font-size: 0.875rem;
      }

      .print-status {
        margin-bottom: 1rem;
      }

      .alert {
        padding: 0.75rem 1rem;
        border-radius: 4px;
        font-size: 0.875rem;
      }

      .alert-success {
        background-color: #d4edda;
        border: 1px solid #c3e6cb;
        color: #155724;
      }

      .alert-danger {
        background-color: #f8d7da;
        border: 1px solid #f5c6cb;
        color: #721c24;
      }

      .preview-container {
        flex: 1;
        display: flex;
        flex-direction: column;
        overflow: hidden;
      }

      .preview-toolbar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.5rem;
        background-color: #f8f9fa;
        border: 1px solid #dee2e6;
        border-bottom: none;
      }

      .zoom-controls {
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      .zoom-level {
        font-size: 0.875rem;
        min-width: 50px;
        text-align: center;
      }

      .preview-content {
        flex: 1;
        overflow: auto;
        border: 1px solid #dee2e6;
        background-color: #e9ecef;
        padding: 2rem;
        transform-origin: top left;
      }

      .preview-frame {
        background: white;
        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        margin: 0 auto;
        min-height: 29.7cm;
      }

      .preview-document.paper-a4 {
        width: 21cm;
        min-height: 29.7cm;
      }

      .preview-document.paper-a5 {
        width: 14.8cm;
        min-height: 21cm;
      }

      .preview-document.paper-letter {
        width: 8.5in;
        min-height: 11in;
      }

      .preview-document.paper-thermal80mm {
        width: 80mm;
        min-height: 200mm;
      }

      .preview-document.orientation-landscape {
        transform: rotate(90deg);
        transform-origin: center;
      }

      .modal-footer {
        display: flex;
        justify-content: flex-end;
        gap: 0.5rem;
        padding: 1rem;
        border-top: 1px solid #dee2e6;
      }

      .btn {
        padding: 0.5rem 1rem;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.875rem;
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      .btn-primary {
        background-color: #007bff;
        color: white;
      }

      .btn-secondary {
        background-color: #6c757d;
        color: white;
      }

      .btn-sm {
        padding: 0.25rem 0.5rem;
        font-size: 0.75rem;
      }

      .btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .spinner {
        width: 1rem;
        height: 1rem;
        border: 2px solid transparent;
        border-top: 2px solid currentColor;
        border-radius: 50%;
        animation: spin 1s linear infinite;
      }

      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrintPreviewComponent implements OnInit {
  @Input() visible = false;
  @Input() previewData: IPrintPreviewData | null = null;
  @Input() allowFormatChange = true;
  @Input() allowCustomSettings = true;

  @Output() closePreview = new EventEmitter<void>();
  @Output() print = new EventEmitter<{
    format: PrintFormat;
    settings: Partial<IPrintSettings>;
  }>();
  @Output() printed = new EventEmitter<IPrintResult>();

  printOptionsForm!: FormGroup;
  showCustomSettings = false;
  zoomLevel = 100;
  isPrinting = false;
  printStatus: IPrintResult | null = null;

  constructor(private fb: FormBuilder, private printService: PrintService) {}

  ngOnInit(): void {
    this.initializeForm();
    this.setupFormSubscriptions();
  }

  private initializeForm(): void {
    this.printOptionsForm = this.fb.group({
      format: [PrintFormat.HTML],
      copies: [this.previewData?.settings.defaultCopies || 1],
      paperSize: [this.previewData?.settings.paperSize || 'A4'],
      orientation: [this.previewData?.settings.orientation || 'portrait'],
      showLogo: [this.previewData?.settings.showLogo ?? true],
      showSignature: [this.previewData?.settings.showSignature ?? true],
      showTerms: [this.previewData?.settings.showTerms ?? true],
    });
  }

  private setupFormSubscriptions(): void {
    // Update preview when settings change
    this.printOptionsForm.valueChanges.subscribe(() => {
      if (this.showCustomSettings) {
        this.refreshPreview();
      }
    });
  }

  get sanitizedHtml(): string {
    if (!this.previewData?.htmlContent) return '';

    // Apply current form settings to HTML
    let html = this.previewData.htmlContent;
    const formValues = this.printOptionsForm.value;

    // Replace settings variables with form values
    Object.keys(formValues).forEach((key) => {
      const value = formValues[key];
      const regex = new RegExp(`{{${key}}}`, 'g');
      html = html.replace(regex, value?.toString() || '');
    });

    return html;
  }

  getPreviewClasses(): string {
    const formValues = this.printOptionsForm.value;
    const classes = ['preview-document'];

    if (formValues.paperSize) {
      classes.push(`paper-${formValues.paperSize.toLowerCase()}`);
    }

    if (formValues.orientation) {
      classes.push(`orientation-${formValues.orientation}`);
    }

    return classes.join(' ');
  }

  zoomIn(): void {
    if (this.zoomLevel < 200) {
      this.zoomLevel += 10;
    }
  }

  zoomOut(): void {
    if (this.zoomLevel > 50) {
      this.zoomLevel -= 10;
    }
  }

  refreshPreview(): void {
    // Trigger change detection to update the preview
    this.printStatus = null;
  }

  async onPrint(): Promise<void> {
    if (!this.previewData) return;

    this.isPrinting = true;
    this.printStatus = null;

    try {
      const formValues = this.printOptionsForm.value;
      const format = formValues.format as PrintFormat;

      // Merge current settings with form values
      const printSettings: Partial<IPrintSettings> = {
        ...this.previewData.settings,
        defaultCopies: formValues.copies,
        paperSize: formValues.paperSize,
        orientation: formValues.orientation,
        showLogo: formValues.showLogo,
        showSignature: formValues.showSignature,
        showTerms: formValues.showTerms,
      };

      // Emit print event for parent component to handle
      this.print.emit({ format, settings: printSettings });

      // If parent doesn't handle it, use the print service directly
      const printableData = {
        id: 'preview',
        type: this.previewData.reportType,
        data: {}, // This should be the actual data
        customSettings: printSettings,
      };

      const result = await this.printService.printReport(printableData, format);
      this.printStatus = result;
      this.printed.emit(result);

      if (result.success) {
        // Close preview after successful print
        setTimeout(() => this.onClose(), 1500);
      }
    } catch (error) {
      this.printStatus = {
        success: false,
        message: 'Print operation failed',
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    } finally {
      this.isPrinting = false;
    }
  }

  onClose(): void {
    this.visible = false;
    this.closePreview.emit();
  }
}
