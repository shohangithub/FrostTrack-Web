import { HttpInterceptorFn } from '@angular/common/http';
import { dateInputToUtc } from '../../utils/date-utils';

/**
 * Recursively converts date-like strings to UTC ISO format
 */
function convertDatesToUtc(obj: any): any {
  if (obj === null || obj === undefined) {
    return obj;
  }

  if (obj instanceof Date) {
    return obj.toISOString();
  }

  if (Array.isArray(obj)) {
    return obj.map((item) => convertDatesToUtc(item));
  }

  if (typeof obj === 'object') {
    const converted: any = {};
    for (const key of Object.keys(obj)) {
      const value = obj[key];

      // Check if the key suggests it's a date field
      const isDateField =
        /date|time|createdAt|updatedAt|validityDate|expiryDate/i.test(key);

      if (isDateField && typeof value === 'string') {
        // Check if it's a date input format (YYYY-MM-DD)
        if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
          converted[key] = dateInputToUtc(value);
        }
        // Check if it's already an ISO string
        else if (/^\d{4}-\d{2}-\d{2}T/.test(value)) {
          converted[key] = value; // Already in ISO format
        } else {
          converted[key] = value;
        }
      } else {
        // Recursively process nested objects and arrays
        converted[key] = convertDatesToUtc(value);
      }
    }
    return converted;
  }

  return obj;
}

/**
 * UTC Date Interceptor
 *
 * Automatically converts date strings in request bodies to UTC ISO format before sending to backend.
 * This ensures all dates sent to the API are in UTC format.
 *
 * The interceptor looks for date patterns in the request body and converts them:
 * - YYYY-MM-DD format (from date inputs) → UTC ISO string
 * - Already ISO strings are left unchanged
 */
export const utcDateInterceptor: HttpInterceptorFn = (req, next) => {
  // Only process requests with a body
  if (!req.body || typeof req.body !== 'object') {
    return next(req);
  }

  // Convert dates in the request body
  const convertedBody = convertDatesToUtc(req.body);

  // Clone the request with converted body
  const modifiedReq = req.clone({
    body: convertedBody,
  });

  return next(modifiedReq);
};
