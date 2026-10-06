import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable, catchError, of, shareReplay } from 'rxjs';

export interface IEditWindowPolicy {
  restrictEditDelete: boolean;
  editDeleteAllowedDays: number;
}

const DAY_MS = 24 * 60 * 60 * 1000;

/**
 * Company-level edit/delete window for bookings, deliveries and transactions.
 * The server enforces the rule; this only hides actions that would be refused.
 */
@Injectable({ providedIn: 'root' })
export class EditWindowService {
  private policy$?: Observable<IEditWindowPolicy>;

  constructor(private http: HttpClient) {}

  /** Pass refresh = true to re-read the policy (e.g. each time a list page opens). */
  getPolicy(refresh = false): Observable<IEditWindowPolicy> {
    if (refresh) this.policy$ = undefined;
    this.policy$ ??= this.http
      .get<IEditWindowPolicy>(`${environment.apiUrl}/company/edit-window-policy`)
      .pipe(
        catchError(() =>
          of({ restrictEditDelete: false, editDeleteAllowedDays: 90 }),
        ),
        shareReplay(1),
      );
    return this.policy$;
  }

  /** Call after company settings change so the next read fetches the new policy. */
  invalidate() {
    this.policy$ = undefined;
  }

  static isWithinWindow(
    policy: IEditWindowPolicy | null | undefined,
    recordDate: Date | string | null | undefined,
  ): boolean {
    if (!policy || !policy.restrictEditDelete || !recordDate) return true;

    const d = new Date(recordDate);
    if (isNaN(d.getTime())) return true;

    const now = new Date();
    const recordDay = Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate());
    const cutoff =
      Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()) -
      policy.editDeleteAllowedDays * DAY_MS;

    return recordDay >= cutoff;
  }
}
