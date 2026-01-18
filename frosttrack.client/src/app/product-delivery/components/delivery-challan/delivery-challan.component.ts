import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
  FormArray,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgSelectModule } from '@ng-select/ng-select';
import { ToastrService } from 'ngx-toastr';
import { DeliveryChallanService } from '../../services/delivery-challan.service';
import { DeliveryService } from '../../services/product-delivery.service';
import {
  IDeliveryChallanRequest,
  IDeliveryChallanResponse,
} from '../../models/delivery-challan.interface';
import { IDeliveryResponse } from '../../models/product-delivery.interface';
import { todayInputFormat } from 'app/utils/date-utils';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-delivery-challan',
  templateUrl: './delivery-challan.component.html',
  styleUrls: [],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgSelectModule],
})
export class DeliveryChallanComponent implements OnInit {
  challanForm: UntypedFormGroup;
  isEditMode = false;
  challanId: string | null = null;
  isLoading = false;
  isSubmitting = false;

  availableDeliveries: IDeliveryResponse[] = [];
  selectedDeliveries: IDeliveryResponse[] = [];

  vehicleTypes = [
    { value: 'Truck', label: 'Truck' },
    { value: 'Van', label: 'Van' },
    { value: 'Pickup', label: 'Pickup' },
    { value: 'Mini Truck', label: 'Mini Truck' },
    { value: 'Lorry', label: 'Lorry' },
    { value: 'Others', label: 'Others' },
  ];

  statusOptions = [
    { value: 'Pending', label: 'Pending' },
    { value: 'In Transit', label: 'In Transit' },
    { value: 'Delivered', label: 'Delivered' },
    { value: 'Cancelled', label: 'Cancelled' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    private challanService: DeliveryChallanService,
    private deliveryService: DeliveryService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    this.challanForm = this.fb.group({
      challanNumber: ['', Validators.required],
      challanDate: [todayInputFormat(), Validators.required],
      vehicleNumber: [
        '',
        [Validators.required, Validators.pattern(/^[A-Z0-9-]+$/i)],
      ],
      driverName: [''],
      driverContact: ['', Validators.pattern(/^[0-9+\-\s()]+$/)],
      vehicleType: [''],
      transportCompany: [''],
      destination: [''],
      remarks: [''],
      status: ['Pending', Validators.required],
      dispatchTime: [null],
      deliveryTime: [null],
    });
  }

  ngOnInit(): void {
    this.challanId = this.route.snapshot.paramMap.get('id');

    if (this.challanId) {
      this.isEditMode = true;
      this.loadChallan();
    } else {
      this.generateChallanNumber();
    }

    this.loadAvailableDeliveries();
  }

  loadChallan(): void {
    if (!this.challanId) return;

    this.isLoading = true;
    this.challanService.getById(this.challanId).subscribe({
      next: (data: IDeliveryChallanResponse) => {
        this.challanForm.patchValue({
          challanNumber: data.challanNumber,
          challanDate: new Date(data.challanDate).toISOString().split('T')[0],
          vehicleNumber: data.vehicleNumber,
          driverName: data.driverName,
          driverContact: data.driverContact,
          vehicleType: data.vehicleType,
          transportCompany: data.transportCompany,
          destination: data.destination,
          remarks: data.remarks,
          status: data.status,
          dispatchTime: data.dispatchTime
            ? new Date(data.dispatchTime).toISOString().slice(0, 16)
            : null,
          deliveryTime: data.deliveryTime
            ? new Date(data.deliveryTime).toISOString().slice(0, 16)
            : null,
        });

        // Load selected deliveries from challan items
        this.loadDeliveriesForChallan(
          data.challanItems.map((item) => item.deliveryId),
        );
        this.isLoading = false;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load delivery challan', 'Error');
        console.error('Error loading challan:', error);
        this.isLoading = false;
        this.router.navigate(['/product-delivery/challan/list']);
      },
    });
  }

  loadDeliveriesForChallan(deliveryIds: string[]): void {
    // Filter from available deliveries or load specifically
    this.selectedDeliveries = this.availableDeliveries.filter((d) =>
      deliveryIds.includes(d.id),
    );
  }

  loadAvailableDeliveries(): void {
    // Load all deliveries using dedicated endpoint
    this.deliveryService.getAllDeliveries().subscribe({
      next: (data: IDeliveryResponse[]) => {
        this.availableDeliveries = data;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load deliveries', 'Error');
        console.error('Error loading deliveries:', error);
      },
    });
  }

  generateChallanNumber(): void {
    this.challanService.generateChallanNumber().subscribe({
      next: (response) => {
        this.challanForm.patchValue({ challanNumber: response.code });
      },
      error: (error: any) => {
        console.error('Error generating challan number:', error);
      },
    });
  }

  onDeliverySelect(delivery: IDeliveryResponse): void {
    if (!this.selectedDeliveries.find((d) => d.id === delivery.id)) {
      this.selectedDeliveries.push(delivery);
    }
  }

  removeDelivery(deliveryId: string): void {
    this.selectedDeliveries = this.selectedDeliveries.filter(
      (d) => d.id !== deliveryId,
    );
  }

  getTotalAmount(): number {
    return this.selectedDeliveries.reduce(
      (sum, delivery) => sum + delivery.chargeAmount,
      0,
    );
  }

  onSubmit(): void {
    if (this.challanForm.invalid) {
      this.challanForm.markAllAsTouched();
      this.toastr.error('Please fill all required fields', 'Validation Error');
      return;
    }

    if (this.selectedDeliveries.length === 0) {
      this.toastr.error(
        'Please select at least one delivery',
        'Validation Error',
      );
      return;
    }

    this.isSubmitting = true;

    const formValue = this.challanForm.value;
    const request: IDeliveryChallanRequest = {
      id: this.challanId || '00000000-0000-0000-0000-000000000000',
      challanNumber: formValue.challanNumber,
      challanDate: new Date(formValue.challanDate),
      vehicleNumber: formValue.vehicleNumber.toUpperCase(),
      driverName: formValue.driverName,
      driverContact: formValue.driverContact,
      vehicleType: formValue.vehicleType,
      transportCompany: formValue.transportCompany,
      destination: formValue.destination,
      branchId: 1, // TODO: Get from user context
      remarks: formValue.remarks,
      status: formValue.status,
      dispatchTime: formValue.dispatchTime
        ? new Date(formValue.dispatchTime)
        : undefined,
      deliveryTime: formValue.deliveryTime
        ? new Date(formValue.deliveryTime)
        : undefined,
      deliveryIds: this.selectedDeliveries.map((d) => d.id),
    };

    const operation = this.isEditMode
      ? this.challanService.update(this.challanId!, request)
      : this.challanService.create(request);

    operation.subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.router.navigate(['/product-delivery/challan/list']);
      },
      error: (error: any) => {
        this.isSubmitting = false;
        console.error('Error saving challan:', error);
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/product-delivery/challan/list']);
  }
}
