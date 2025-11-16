import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-print-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="print-header">
      <div *ngIf="showLogo && logoUrl" class="company-logo">
        <img [src]="logoUrl" [alt]="companyName + ' Logo'" />
      </div>
      <div class="company-info">
        <h2 *ngIf="companyName">{{ companyName }}</h2>
        <p *ngIf="companyAddress">{{ companyAddress }}</p>
        <p *ngIf="companyPhone || companyEmail">
          <span *ngIf="companyPhone">Phone: {{ companyPhone }}</span>
          <span *ngIf="companyPhone && companyEmail"> | </span>
          <span *ngIf="companyEmail">Email: {{ companyEmail }}</span>
        </p>
        <p *ngIf="companyWebsite">Website: {{ companyWebsite }}</p>
      </div>
    </div>
  `,
  styles: [
    `
      .print-header {
        text-align: center;
        border-bottom: 2px solid #007bff;
        padding-bottom: 20px;
        margin-bottom: 30px;
      }
      .company-logo img {
        max-height: 80px;
        margin-bottom: 10px;
      }
      .company-info h2 {
        margin: 0;
        color: #007bff;
      }
      .company-info p {
        margin: 5px 0;
        color: #666;
      }
    `,
  ],
})
export class PrintHeaderComponent {
  @Input() companyName?: string;
  @Input() companyAddress?: string;
  @Input() companyPhone?: string;
  @Input() companyEmail?: string;
  @Input() companyWebsite?: string;
  @Input() logoUrl?: string;
  @Input() showLogo = true;
}
