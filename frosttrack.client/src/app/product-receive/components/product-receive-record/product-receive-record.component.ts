import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProductReceiveService } from 'app/product-receive/services/product-receive.service';
import { IProductReceiveResponse } from 'app/product-receive/models/product-receive.interface';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-product-receive-record',
  templateUrl: './product-receive-record.component.html',
  standalone: true,
  imports: [RouterLink, CommonModule],
  providers: [ProductReceiveService],
})
export class ProductReceiveRecordComponent implements OnInit {
  productReceive: IProductReceiveResponse | null = null;
  isLoading = true;

  constructor(
    private productReceiveService: ProductReceiveService,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadProductReceive(Number(id));
    }
  }

  loadProductReceive(id: number) {
    this.isLoading = true;
    this.productReceiveService.getById(id).subscribe({
      next: (response) => {
        this.productReceive = response;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error('Failed to load product receive record');
      },
    });
  }

  print() {
    window.print();
  }
}
