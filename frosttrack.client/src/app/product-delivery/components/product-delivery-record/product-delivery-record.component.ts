import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProductDeliveryService } from 'app/product-delivery/services/product-delivery.service';
import { IProductDeliveryResponse } from 'app/product-delivery/models/product-delivery.interface';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-product-delivery-record',
  templateUrl: './product-delivery-record.component.html',
  standalone: true,
  imports: [RouterLink, CommonModule],
  providers: [ProductDeliveryService],
})
export class ProductDeliveryRecordComponent implements OnInit {
  productDelivery: IProductDeliveryResponse | null = null;
  isLoading = true;

  constructor(
    private productDeliveryService: ProductDeliveryService,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadProductDelivery(Number(id));
    }
  }

  loadProductDelivery(id: number) {
    this.isLoading = true;
    this.productDeliveryService.getById(id).subscribe({
      next: (response) => {
        this.productDelivery = response;
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
