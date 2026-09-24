import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import {
  ExecuteSeasonArchiveRequest,
  SeasonArchiveHistoryResponse,
  SeasonArchivePreviewResponse,
  SeasonArchiveResultResponse,
} from '../models/season-archive.model';

@Injectable({ providedIn: 'root' })
export class SeasonArchiveService {
  private readonly baseUrl = `${environment.apiUrl}/SeasonArchive`;

  constructor(private readonly http: HttpClient) {}

  getPreview(cutoffDate?: string): Observable<SeasonArchivePreviewResponse> {
    const url = cutoffDate
      ? `${this.baseUrl}/preview?cutoffDate=${encodeURIComponent(cutoffDate)}`
      : `${this.baseUrl}/preview`;
    return this.http.get<SeasonArchivePreviewResponse>(url);
  }

  executeArchive(
    request: ExecuteSeasonArchiveRequest
  ): Observable<SeasonArchiveResultResponse> {
    return this.http.post<SeasonArchiveResultResponse>(
      `${this.baseUrl}/execute`,
      request
    );
  }

  getHistory(): Observable<SeasonArchiveHistoryResponse[]> {
    return this.http.get<SeasonArchiveHistoryResponse[]>(
      `${this.baseUrl}/history`
    );
  }
}
