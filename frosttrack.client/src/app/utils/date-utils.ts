/**
 * UTC Date Utilities for FrostTrack Application
 *
 * Production-grade date handling ensuring UTC consistency across the entire application.
 * All dates are stored as UTC in the backend and converted to local timezone only for display.
 */

/**
 * Converts a local Date object to UTC ISO string for backend API calls
 * @param date - Local date to convert
 * @returns ISO 8601 UTC string (e.g., "2025-01-15T10:30:00.000Z")
 */
export function toUtcIsoString(
  date: Date | string | null | undefined
): string | null {
  if (!date) return null;

  const d = typeof date === 'string' ? new Date(date) : date;
  if (isNaN(d.getTime())) return null;

  return d.toISOString();
}

/**
 * Converts a UTC date string from backend to local Date object
 * @param utcString - UTC ISO string from backend
 * @returns Local Date object
 */
export function fromUtcString(
  utcString: string | null | undefined
): Date | null {
  if (!utcString) return null;

  const date = new Date(utcString);
  if (isNaN(date.getTime())) return null;

  return date;
}

/**
 * Converts a date input value (YYYY-MM-DD) to UTC ISO string at start of day
 * Used for date inputs that don't include time
 * @param dateString - Date string in YYYY-MM-DD format
 * @returns UTC ISO string at 00:00:00 local time
 */
export function dateInputToUtc(
  dateString: string | null | undefined
): string | null {
  if (!dateString) return null;

  // Parse as local date at midnight
  const [year, month, day] = dateString.split('-').map(Number);
  const date = new Date(year, month - 1, day, 0, 0, 0, 0);

  if (isNaN(date.getTime())) return null;

  return date.toISOString();
}

/**
 * Converts a UTC ISO string to date input format (YYYY-MM-DD)
 * Used for displaying dates in date inputs
 * @param utcString - UTC ISO string
 * @returns Date string in YYYY-MM-DD format (local timezone)
 */
export function utcToDateInput(
  utcString: string | null | undefined
): string | null {
  if (!utcString) return null;

  const date = new Date(utcString);
  if (isNaN(date.getTime())) return null;

  // Format as YYYY-MM-DD in local timezone
  const year = date.getFullYear();
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const day = date.getDate().toString().padStart(2, '0');

  return `${year}-${month}-${day}`;
}

/**
 * Formats a UTC date string for display in local timezone
 * @param utcString - UTC ISO string
 * @param format - Format type: 'short' | 'medium' | 'long' | 'full'
 * @returns Formatted date string in local timezone
 */
export function formatUtcDate(
  utcString: string | null | undefined,
  format: 'short' | 'medium' | 'long' | 'full' = 'medium'
): string {
  if (!utcString) return '';

  const date = new Date(utcString);
  if (isNaN(date.getTime())) return '';

  const formatOptions: Record<string, Intl.DateTimeFormatOptions> = {
    short: { year: 'numeric', month: '2-digit', day: '2-digit' },
    medium: { year: 'numeric', month: 'short', day: '2-digit' },
    long: {
      year: 'numeric',
      month: 'long',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    },
    full: {
      year: 'numeric',
      month: 'long',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      timeZoneName: 'short',
    },
  };

  return date.toLocaleDateString(undefined, formatOptions[format]);
}

/**
 * Gets the current UTC date as ISO string
 * @returns Current UTC ISO string
 */
export function nowUtc(): string {
  return new Date().toISOString();
}

/**
 * Gets the current date in YYYY-MM-DD format for date inputs
 * @returns Current date in YYYY-MM-DD format (local timezone)
 */
export function todayInputFormat(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = (now.getMonth() + 1).toString().padStart(2, '0');
  const day = now.getDate().toString().padStart(2, '0');

  return `${year}-${month}-${day}`;
}

/**
 * Gets the current date in YYYY-MM-DD format for date inputs
 * @returns Current date in YYYY-MM-DD format (local timezone)
 */
export function dateInputFormat(date: Date): string {
  const now = date;
  const year = now.getFullYear();
  const month = (now.getMonth() + 1).toString().padStart(2, '0');
  const day = now.getDate().toString().padStart(2, '0');

  return `${year}-${month}-${day}`;
}

/**
 * Adds days to a UTC date string
 * @param utcString - UTC ISO string
 * @param days - Number of days to add (can be negative)
 * @returns New UTC ISO string
 */
export function addDaysUtc(utcString: string, days: number): string {
  const date = new Date(utcString);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString();
}

/**
 * Checks if a UTC date string is in the past
 * @param utcString - UTC ISO string
 * @returns True if the date is in the past
 */
export function isInPast(utcString: string): boolean {
  const date = new Date(utcString);
  return date.getTime() < Date.now();
}

/**
 * Checks if a UTC date string is in the future
 * @param utcString - UTC ISO string
 * @returns True if the date is in the future
 */
export function isInFuture(utcString: string): boolean {
  const date = new Date(utcString);
  return date.getTime() > Date.now();
}

/**
 * Gets the start of day in UTC for a given date
 * @param date - Date or UTC string
 * @returns UTC ISO string at 00:00:00 UTC
 */
export function startOfDayUtc(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  d.setUTCHours(0, 0, 0, 0);
  return d.toISOString();
}

/**
 * Gets the end of day in UTC for a given date
 * @param date - Date or UTC string
 * @returns UTC ISO string at 23:59:59.999 UTC
 */
export function endOfDayUtc(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  d.setUTCHours(23, 59, 59, 999);
  return d.toISOString();
}
