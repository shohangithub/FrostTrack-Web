import { Pipe, PipeTransform } from '@angular/core';
import { formatUtcDate } from '../../utils/date-utils';

/**
 * UTC Date Pipe - Converts UTC dates from backend to local timezone for display
 *
 * Usage:
 *   {{ utcDateString | utcDate }}              // Default: medium format
 *   {{ utcDateString | utcDate:'short' }}      // Short: 01/15/2025
 *   {{ utcDateString | utcDate:'medium' }}     // Medium: Jan 15, 2025
 *   {{ utcDateString | utcDate:'long' }}       // Long: January 15, 2025, 10:30 AM
 *   {{ utcDateString | utcDate:'full' }}       // Full: January 15, 2025, 10:30:45 AM GMT+6
 */
@Pipe({
  name: 'utcDate',
  standalone: true,
})
export class UtcDatePipe implements PipeTransform {
  transform(
    value: string | null | undefined,
    format: 'short' | 'medium' | 'long' | 'full' = 'medium'
  ): string {
    return formatUtcDate(value, format);
  }
}

/**
 * UTC Date Input Pipe - Converts UTC dates to YYYY-MM-DD format for date inputs
 *
 * Usage:
 *   <input type="date" [value]="utcDateString | utcDateInput" />
 */
@Pipe({
  name: 'utcDateInput',
  standalone: true,
})
export class UtcDateInputPipe implements PipeTransform {
  transform(value: string | null | undefined): string | null {
    if (!value) return null;

    const date = new Date(value);
    if (isNaN(date.getTime())) return null;

    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}

/**
 * UTC Time Pipe - Displays only time portion from UTC datetime
 *
 * Usage:
 *   {{ utcDateString | utcTime }}              // 10:30 AM
 *   {{ utcDateString | utcTime:'24' }}         // 10:30
 *   {{ utcDateString | utcTime:'full' }}       // 10:30:45 AM
 */
@Pipe({
  name: 'utcTime',
  standalone: true,
})
export class UtcTimePipe implements PipeTransform {
  transform(
    value: string | null | undefined,
    format: '12' | '24' | 'full' = '12'
  ): string {
    if (!value) return '';

    const date = new Date(value);
    if (isNaN(date.getTime())) return '';

    const optionsMap: Record<string, Intl.DateTimeFormatOptions> = {
      '12': { hour: '2-digit', minute: '2-digit', hour12: true },
      '24': { hour: '2-digit', minute: '2-digit', hour12: false },
      full: {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: true,
      },
    };

    const options = optionsMap[format];

    return date.toLocaleTimeString(undefined, options);
  }
}

/**
 * Relative Time Pipe - Shows relative time (e.g., "2 hours ago", "in 3 days")
 *
 * Usage:
 *   {{ utcDateString | relativeTime }}
 */
@Pipe({
  name: 'relativeTime',
  standalone: true,
})
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';

    const date = new Date(value);
    if (isNaN(date.getTime())) return '';

    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSec = Math.floor(diffMs / 1000);
    const diffMin = Math.floor(diffSec / 60);
    const diffHour = Math.floor(diffMin / 60);
    const diffDay = Math.floor(diffHour / 24);
    const diffWeek = Math.floor(diffDay / 7);
    const diffMonth = Math.floor(diffDay / 30);
    const diffYear = Math.floor(diffDay / 365);

    if (diffSec < 60) return 'Just now';
    if (diffMin < 60) return `${diffMin} minute${diffMin > 1 ? 's' : ''} ago`;
    if (diffHour < 24) return `${diffHour} hour${diffHour > 1 ? 's' : ''} ago`;
    if (diffDay < 7) return `${diffDay} day${diffDay > 1 ? 's' : ''} ago`;
    if (diffWeek < 4) return `${diffWeek} week${diffWeek > 1 ? 's' : ''} ago`;
    if (diffMonth < 12)
      return `${diffMonth} month${diffMonth > 1 ? 's' : ''} ago`;
    return `${diffYear} year${diffYear > 1 ? 's' : ''} ago`;
  }
}
