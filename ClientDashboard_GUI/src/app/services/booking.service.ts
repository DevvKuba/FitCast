import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { ApiResponse } from '../models/api-response';
import { BookedSessionSlotViewDto } from '../models/dtos/booked-session-slot-view-dto';
import { BookedSessionSlotDto } from '../models/dtos/booked-session-slot-dto';
import { BookingSeriesDto } from '../models/dtos/booking-series-dto';
import { BookingSeriesCreateDto } from '../models/dtos/booking-series-create-dto';
import { BookingSeriesUpdateDto } from '../models/dtos/booking-series-update-dto';

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  http = inject(HttpClient);
  baseUrl = environment.apiUrl;

  // Both values must be UTC ISO strings ending in "Z" - the API rejects anything else.
  // The range can be at most 42 days.
  getTrainerBookedSessionSlots(fromUtc: string, toUtc: string) : Observable<ApiResponse<BookedSessionSlotViewDto[]>> {
    return this.http.get<ApiResponse<BookedSessionSlotViewDto[]>>(this.baseUrl + `bookedSessionSlot/getTrainerBookedSessionSlots?fromUtc=${fromUtc}&toUtc=${toUtc}`);
  }

  addBookedSessionSlot(sessionSlot: BookedSessionSlotDto) : Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(this.baseUrl + `bookedSessionSlot/addBookedSessionSlot`, sessionSlot);
  }

  updateBookedSessionSlot(bookedSessionSlotId: number, sessionSlot: BookedSessionSlotDto) : Observable<ApiResponse<string>> {
    return this.http.put<ApiResponse<string>>(this.baseUrl + `bookedSessionSlot/updateBookedSessionSlot?bookedSessionSlotId=${bookedSessionSlotId}`, sessionSlot);
  }

  deleteBookedSessionSlot(bookedSessionSlotId: number) : Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(this.baseUrl + `bookedSessionSlot/deleteBookedSessionSlot?bookedSessionSlotId=${bookedSessionSlotId}`);
  }

  // ---- Booking series ----

  getTrainerBookingSeries() : Observable<ApiResponse<BookingSeriesDto[]>> {
    return this.http.get<ApiResponse<BookingSeriesDto[]>>(this.baseUrl + `bookingSeries/getTrainerBookingSeries`);
  }

  getBookingSeriesById(bookingSeriesId: number) : Observable<ApiResponse<BookingSeriesDto>> {
    return this.http.get<ApiResponse<BookingSeriesDto>>(this.baseUrl + `bookingSeries/getBookingSeriesById?bookingSeriesId=${bookingSeriesId}`);
  }

  createBookingSeries(bookingSeries: BookingSeriesCreateDto) : Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(this.baseUrl + `bookingSeries/createBookingSeries`, bookingSeries);
  }

  // Replaces the series' upcoming scheduled sessions from the new start date
  updateBookingSeries(bookingSeriesId: number, bookingSeries: BookingSeriesUpdateDto) : Observable<ApiResponse<string>> {
    return this.http.put<ApiResponse<string>>(this.baseUrl + `bookingSeries/updateBookingSeries?bookingSeriesId=${bookingSeriesId}`, bookingSeries);
  }

  // Removes upcoming scheduled sessions; past sessions stay as one-offs
  deleteBookingSeries(bookingSeriesId: number) : Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(this.baseUrl + `bookingSeries/deleteBookingSeries?bookingSeriesId=${bookingSeriesId}`);
  }
}
