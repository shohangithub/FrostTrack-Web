import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IBookingPaymentDetail } from '../../models/booking-payment.interface';

/**
 * Read-only, report-style account of one booking: booking info, items &amp; stock, charge ledger,
 * deliveries, labour and payments. Shared by Customer Payment and Discount &amp; Adjustment.
 */
@Component({
  selector: 'app-booking-account-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './booking-account-detail.component.html',
  styleUrls: ['./booking-account-detail.component.scss'],
})
export class BookingAccountDetailComponent {
  @Input({ required: true }) detail!: IBookingPaymentDetail;

  kindLabel(kind: string): string {
    switch (kind) {
      case 'AT_DELIVERY':
        return 'At delivery';
      case 'ADVANCE':
        return 'Advance applied';
      case 'DISCOUNT':
        return 'Adjustment (−)';
      default:
        return 'Payment';
    }
  }

  chargeLabel(chargeType: string, cycleNo?: number | null, source?: string): string {
    switch (chargeType) {
      case 'RENT_CYCLE':
        return `Rent - month ${cycleNo ?? ''}`;
      case 'BOOKING_LABOUR':
        return 'Booking labour';
      case 'DELIVERY_LABOUR':
        return 'Delivery labour';
      case 'DELIVERY_ADJUSTMENT':
        return 'Delivery adjustment';
      case 'MANUAL_ADJUSTMENT':
        return source === 'REVERSAL' ? 'Adjustment (+) cancelled' : 'Adjustment (+)';
      default:
        return chargeType;
    }
  }
}
