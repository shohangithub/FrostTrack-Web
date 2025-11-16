import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-print-footer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="print-footer">
      <div *ngIf="showTerms && termsAndConditions" class="terms">
        <p>{{ termsAndConditions }}</p>
      </div>
      <div *ngIf="footerText" class="footer-text">
        <p>{{ footerText }}</p>
      </div>
      <div
        *ngIf="showSignature && (authorizedBy || signature)"
        class="signature"
      >
        <p *ngIf="authorizedBy">Authorized by: {{ authorizedBy }}</p>
        <div *ngIf="signature" class="signature-image">
          <img [src]="signature" alt="Signature" />
        </div>
      </div>
      <div *ngIf="thankYouMessage" class="thank-you">
        <p>{{ thankYouMessage }}</p>
      </div>
    </div>
  `,
  styles: [
    `
      .print-footer {
        border-top: 1px solid #ddd;
        padding-top: 20px;
        margin-top: 30px;
        text-align: center;
        font-size: 12px;
        color: #666;
      }
      .terms {
        margin-bottom: 15px;
        font-style: italic;
      }
      .footer-text {
        margin-bottom: 15px;
      }
      .signature {
        margin: 20px 0;
        text-align: right;
      }
      .signature-image img {
        max-height: 50px;
        margin-top: 10px;
      }
      .thank-you {
        font-weight: bold;
        color: #007bff;
      }
    `,
  ],
})
export class PrintFooterComponent {
  @Input() footerText?: string;
  @Input() termsAndConditions?: string;
  @Input() thankYouMessage?: string;
  @Input() authorizedBy?: string;
  @Input() signature?: string;
  @Input() showTerms = true;
  @Input() showSignature = true;
}
