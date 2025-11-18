import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { DeliveryService } from 'app/product-delivery/services/product-delivery.service';
import { IDeliveryResponse } from 'app/product-delivery/models/product-delivery.interface';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-product-delivery-record',
  templateUrl: './product-delivery-record.component.html',
  standalone: true,
  imports: [RouterLink, CommonModule],
  providers: [DeliveryService],
})
export class DeliveryRecordComponent implements OnInit {
  delivery: IDeliveryResponse | null = null;
  isLoading = true;

  constructor(
    private deliveryService: DeliveryService,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadDelivery(id);
    }
  }

  loadDelivery(id: string) {
    this.isLoading = true;
    this.deliveryService.getById(id).subscribe({
      next: (response) => {
        this.delivery = response;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error('Failed to load product delivery record');
      },
    });
  }

  print() {
    window.print();
  }
}
