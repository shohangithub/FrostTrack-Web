import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { BookingService } from 'app/booking/services/booking.service';
import { IBookingResponse } from 'app/booking/models/booking.interface';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-booking-record',
  templateUrl: './booking-record.component.html',
  standalone: true,
  imports: [RouterLink, CommonModule],
  providers: [BookingService],
})
export class BookingRecordComponent implements OnInit {
  Booking: IBookingResponse | null = null;
  isLoading = true;

  constructor(
    private BookingService: BookingService,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadBooking(id);
    }
  }

  loadBooking(id: string) {
    this.isLoading = true;
    this.BookingService.getById(id).subscribe({
      next: (response) => {
        this.Booking = response;
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
