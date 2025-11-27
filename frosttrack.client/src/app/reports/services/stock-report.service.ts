import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import {
  IStockReportItem,
  ICustomerStockReport,
  IProductStockReport,
  IStockSummary,
} from '../models/stock-report.interface';

@Injectable({ providedIn: 'root' })
export class StockReportService {
  private path: string = `${environment.apiUrl}/StockReport`;

  constructor(private http: HttpClient) {}

  getStockReport(
    startDate: Date,
    endDate: Date,
    customerId?: number,
    productId?: number
  ): Observable<IStockReportItem[]> {
    let params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    if (customerId) {
      params = params.set('customerId', customerId.toString());
    }
    if (productId) {
      params = params.set('productId', productId.toString());
    }

    return this.http.get<IStockReportItem[]>(`${this.path}`, { params });
  }

  getCustomerStockReport(
    startDate: Date,
    endDate: Date
  ): Observable<ICustomerStockReport[]> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    return this.http.get<ICustomerStockReport[]>(`${this.path}/by-customer`, {
      params,
    });
  }

  getProductStockReport(
    startDate: Date,
    endDate: Date
  ): Observable<IProductStockReport[]> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    return this.http.get<IProductStockReport[]>(`${this.path}/by-product`, {
      params,
    });
  }

  getStockSummary(startDate: Date, endDate: Date): Observable<IStockSummary> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    return this.http.get<IStockSummary>(`${this.path}/summary`, { params });
  }

  exportToCSV(data: IStockReportItem[], filename: string): void {
    const headers = [
      'Booking Number',
      'Booking Date',
      'Customer',
      'Product',
      'Booked Qty',
      'Delivered Qty',
      'Remaining Qty',
      'Unit',
      'Rate',
      'Total Value',
      'Status',
      'Last Delivery',
    ];

    const csvData = data.map((item) => [
      item.bookingNumber,
      new Date(item.bookingDate).toLocaleDateString(),
      item.customerName,
      item.productName,
      item.bookingQuantity,
      item.deliveredQuantity,
      item.remainingQuantity,
      item.unitName,
      item.bookingRate,
      item.totalValue,
      item.status,
      item.lastDeliveryDate
        ? new Date(item.lastDeliveryDate).toLocaleDateString()
        : 'N/A',
    ]);

    const csv = [headers, ...csvData].map((row) => row.join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${filename}_${new Date().getTime()}.csv`;
    link.click();
    window.URL.revokeObjectURL(url);
  }
}
