import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IPrintHeader, IPrintFooter } from '../../services/print.service';

export interface IPaymentReceiptData {
  id: number;
  paymentNumber: string;
  paymentDate: string;
  paymentType: string;
  supplier: {
    name: string;
    phone?: string;
    address?: string;
    email?: string;
  };
  paymentMethod: string;
  paymentDetails?: {
    bankName?: string;
    checkNumber?: string;
    checkDate?: string;
    onlinePaymentMethod?: string;
    transactionId?: string;
    gatewayReference?: string;
    mobileWalletType?: string;
    walletNumber?: string;
    walletTransactionId?: string;
    cardType?: string;
    cardLastFour?: string;
    cardTransactionId?: string;
  };
  paymentAmount: number;
  previousDue: number;
  remainingDue: number;
  notes?: string;
  createdBy: string;
  branchName: string;
  printDateTime: string;
}

@Component({
  selector: 'app-payment-receipt',
  template: `
    <div class="print-container">
      <!-- Header -->
      <div class="print-header">
        <div *ngIf="header.logo && showLogo" class="company-logo mb-2">
          <img
            [src]="header.logo"
            alt="Company Logo"
            style="max-height: 60px;"
          />
        </div>

        <h1>{{ header.companyName }}</h1>

        <div class="company-info">
          <div *ngIf="header.companyAddress">{{ header.companyAddress }}</div>
          <div *ngIf="header.companyPhone">
            Phone: {{ header.companyPhone }}
          </div>
          <div *ngIf="header.companyEmail">
            Email: {{ header.companyEmail }}
          </div>
          <div *ngIf="header.companyWebsite">
            Web: {{ header.companyWebsite }}
          </div>
        </div>

        <div
          *ngIf="showBranchInfo && header.branchName"
          class="branch-info mt-2"
        >
          <hr style="margin: 10px 0; border: 1px solid #ccc;" />
          <div>
            <strong>Branch: {{ header.branchName }}</strong>
          </div>
          <div *ngIf="header.branchAddress">{{ header.branchAddress }}</div>
          <div *ngIf="header.branchPhone">Phone: {{ header.branchPhone }}</div>
        </div>

        <h2 style="margin: 15px 0 10px 0; font-size: 18px;">PAYMENT RECEIPT</h2>
      </div>

      <!-- Content -->
      <div class="print-content">
        <!-- Receipt Info -->
        <div class="receipt-info mb-3">
          <div class="row">
            <span class="col-label">Receipt No:</span>
            <span class="col-value">{{ receiptData.paymentNumber }}</span>
          </div>
          <div class="row">
            <span class="col-label">Payment Date:</span>
            <span class="col-value">{{
              receiptData.paymentDate | date : 'dd/MM/yyyy'
            }}</span>
          </div>
          <div class="row">
            <span class="col-label">Print Date:</span>
            <span class="col-value">{{
              receiptData.printDateTime | date : 'dd/MM/yyyy HH:mm'
            }}</span>
          </div>
          <div class="row">
            <span class="col-label">Payment Type:</span>
            <span class="col-value">{{ receiptData.paymentType }}</span>
          </div>
        </div>

        <!-- Supplier Info -->
        <div class="supplier-info mb-3">
          <h3
            style="margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;"
          >
            Supplier Information
          </h3>
          <div class="row">
            <span class="col-label">Supplier Name:</span>
            <span class="col-value">{{ receiptData.supplier.name }}</span>
          </div>
          <div class="row" *ngIf="receiptData.supplier.phone">
            <span class="col-label">Phone:</span>
            <span class="col-value">{{ receiptData.supplier.phone }}</span>
          </div>
          <div class="row" *ngIf="receiptData.supplier.address">
            <span class="col-label">Address:</span>
            <span class="col-value">{{ receiptData.supplier.address }}</span>
          </div>
          <div class="row" *ngIf="receiptData.supplier.email">
            <span class="col-label">Email:</span>
            <span class="col-value">{{ receiptData.supplier.email }}</span>
          </div>
        </div>

        <!-- Payment Details -->
        <div class="payment-details mb-3">
          <h3
            style="margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;"
          >
            Payment Details
          </h3>

          <table class="receipt-table">
            <tr>
              <td><strong>Payment Method:</strong></td>
              <td>{{ receiptData.paymentMethod }}</td>
            </tr>

            <!-- Bank Details -->
            <tr *ngIf="receiptData.paymentDetails?.bankName">
              <td><strong>Bank Name:</strong></td>
              <td>{{ receiptData.paymentDetails?.bankName }}</td>
            </tr>

            <!-- Check Details -->
            <tr *ngIf="receiptData.paymentDetails?.checkNumber">
              <td><strong>Check Number:</strong></td>
              <td>{{ receiptData.paymentDetails?.checkNumber }}</td>
            </tr>
            <tr *ngIf="receiptData.paymentDetails?.checkDate">
              <td><strong>Check Date:</strong></td>
              <td>
                {{
                  receiptData.paymentDetails?.checkDate | date : 'dd/MM/yyyy'
                }}
              </td>
            </tr>

            <!-- Online Payment Details -->
            <tr *ngIf="receiptData.paymentDetails?.onlinePaymentMethod">
              <td><strong>Online Method:</strong></td>
              <td>{{ receiptData.paymentDetails?.onlinePaymentMethod }}</td>
            </tr>
            <tr *ngIf="receiptData.paymentDetails?.transactionId">
              <td><strong>Transaction ID:</strong></td>
              <td>{{ receiptData.paymentDetails?.transactionId }}</td>
            </tr>
            <tr *ngIf="receiptData.paymentDetails?.gatewayReference">
              <td><strong>Gateway Reference:</strong></td>
              <td>{{ receiptData.paymentDetails?.gatewayReference }}</td>
            </tr>

            <!-- Mobile Wallet Details -->
            <tr *ngIf="receiptData.paymentDetails?.mobileWalletType">
              <td><strong>Wallet Type:</strong></td>
              <td>{{ receiptData.paymentDetails?.mobileWalletType }}</td>
            </tr>
            <tr *ngIf="receiptData.paymentDetails?.walletNumber">
              <td><strong>Wallet Number:</strong></td>
              <td>{{ receiptData.paymentDetails?.walletNumber }}</td>
            </tr>
            <tr *ngIf="receiptData.paymentDetails?.walletTransactionId">
              <td><strong>Wallet Transaction ID:</strong></td>
              <td>{{ receiptData.paymentDetails?.walletTransactionId }}</td>
            </tr>

            <!-- Card Details -->
            <tr *ngIf="receiptData.paymentDetails?.cardType">
              <td><strong>Card Type:</strong></td>
              <td>{{ receiptData.paymentDetails?.cardType }}</td>
            </tr>
            <tr *ngIf="receiptData.paymentDetails?.cardLastFour">
              <td><strong>Card Number:</strong></td>
              <td>
                ****-****-****-{{ receiptData.paymentDetails?.cardLastFour }}
              </td>
            </tr>
            <tr *ngIf="receiptData.paymentDetails?.cardTransactionId">
              <td><strong>Card Transaction ID:</strong></td>
              <td>{{ receiptData.paymentDetails?.cardTransactionId }}</td>
            </tr>
          </table>
        </div>

        <!-- Amount Summary -->
        <div class="amount-summary mb-3">
          <h3
            style="margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;"
          >
            Amount Summary
          </h3>

          <table class="receipt-table">
            <tr>
              <td><strong>Previous Due Amount:</strong></td>
              <td class="text-right">
                {{
                  receiptData.previousDue
                    | currency : 'USD' : 'symbol' : '1.2-2'
                }}
              </td>
            </tr>
            <tr class="amount-row">
              <td><strong>Payment Amount:</strong></td>
              <td class="text-right">
                <strong>{{
                  receiptData.paymentAmount
                    | currency : 'USD' : 'symbol' : '1.2-2'
                }}</strong>
              </td>
            </tr>
            <tr>
              <td><strong>Remaining Due:</strong></td>
              <td class="text-right">
                {{
                  receiptData.remainingDue
                    | currency : 'USD' : 'symbol' : '1.2-2'
                }}
              </td>
            </tr>
          </table>
        </div>

        <!-- Notes -->
        <div class="notes mb-3" *ngIf="receiptData.notes">
          <h3
            style="margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;"
          >
            Notes
          </h3>
          <p
            style="margin: 0; padding: 10px; background: #f8f9fa; border-left: 4px solid #007bff;"
          >
            {{ receiptData.notes }}
          </p>
        </div>

        <!-- Additional Info -->
        <div class="additional-info">
          <div class="row">
            <span class="col-label">Processed By:</span>
            <span class="col-value">{{ receiptData.createdBy }}</span>
          </div>
          <div class="row">
            <span class="col-label">Branch:</span>
            <span class="col-value">{{ receiptData.branchName }}</span>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="print-footer">
        <div *ngIf="footer.thankYouMessage" class="mb-2">
          <strong>{{ footer.thankYouMessage }}</strong>
        </div>

        <div *ngIf="footer.footerText" class="mb-2">
          {{ footer.footerText }}
        </div>

        <div
          *ngIf="footer.termsAndConditions"
          class="mb-2"
          style="font-size: 10px;"
        >
          <em>{{ footer.termsAndConditions }}</em>
        </div>

        <div
          *ngIf="footer.authorizedBy || footer.signature"
          class="signature-section mt-3"
        >
          <div
            style="border-top: 1px solid #333; width: 200px; margin: 20px auto 5px auto;"
          ></div>
          <div *ngIf="footer.authorizedBy">
            Authorized by: {{ footer.authorizedBy }}
          </div>
          <div *ngIf="footer.signature">{{ footer.signature }}</div>
        </div>
      </div>
    </div>
  `,
  standalone: true,
  imports: [CommonModule],
})
export class PaymentReceiptComponent {
  @Input() receiptData!: IPaymentReceiptData;
  @Input() header: IPrintHeader = {
    companyName: 'Your Company Name',
    companyAddress: 'Your Company Address',
    companyPhone: 'Your Phone Number',
    companyEmail: 'your@email.com',
  };
  @Input() footer: IPrintFooter = {
    footerText: 'Thank you for your business!',
    thankYouMessage: 'Thank you for your payment!',
  };
  @Input() showLogo: boolean = true;
  @Input() showBranchInfo: boolean = true;
}
