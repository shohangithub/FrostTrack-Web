import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { DeliveryChallanService } from '../../services/delivery-challan.service';
import { IDeliveryChallanResponse } from '../../models/delivery-challan.interface';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';

@Component({
  selector: 'app-delivery-challan-print',
  templateUrl: './delivery-challan-print.component.html',
  styleUrls: ['./delivery-challan-print.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class DeliveryChallanPrintComponent implements OnInit {
  challan: IDeliveryChallanResponse | null = null;
  isLoading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private challanService: DeliveryChallanService,
    private toastr: ToastrService,
  ) {}

  ngOnInit(): void {
    const challanId = this.route.snapshot.paramMap.get('id');
    if (challanId) {
      this.loadChallan(challanId);
    }
  }

  loadChallan(id: string): void {
    this.isLoading = true;
    this.challanService.getById(id).subscribe({
      next: (data: IDeliveryChallanResponse) => {
        this.challan = data;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load delivery challan', 'Error');
        console.error('Error loading challan:', error);
        this.isLoading = false;
        this.goBack();
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/product-delivery/challan/list']);
  }

  getTotalAmount(): number {
    if (!this.challan) return 0;
    return this.challan.challanItems.reduce(
      (sum, item) => sum + item.chargeAmount,
      0,
    );
  }
}
