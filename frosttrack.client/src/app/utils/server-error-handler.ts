export const formatErrorMessage = (response: ErrorResponse): string => {
  // Handle null/undefined response
  if (!response) {
    return 'Something went wrong! Please try again.';
  }

  // Handle case where response.error exists
  if (response.error) {
    const errors = response.error.errors;

    // Check if errors object exists and has properties
    if (!errors || typeof errors !== 'object') {
      return (
        response.error.message || 'Something went wrong! Please try again.'
      );
    }

    const errorKeys = Object.keys(errors);
    if (errorKeys.length === 0) {
      return (
        response.error.message || 'Something went wrong! Please try again.'
      );
    }

    // Format validation errors without HTML
    const errorMessages: string[] = [];
    errorKeys.forEach((key) => {
      const fieldErrors = Array.isArray(errors[key])
        ? (errors[key] as string[])
        : [errors[key] as string];
      errorMessages.push(...fieldErrors);
    });

    return errorMessages.join('. ');
  }

  // Handle direct error message
  if (response.message) {
    return response.message;
  }

  // Fallback for unknown error structure
  return 'An unexpected error occurred. Please try again.';
};

export interface ServerError {
  errors?: { [key: string]: string[] | string };
  types?: string;
  title?: string;
  status?: number;
  isValidFailed?: boolean;
  message?: string;
  traceId?: string;
}

export interface ErrorResponse {
  error?: ServerError;
  headers?: any;
  message?: string;
  name?: string;
  ok?: boolean;
  status?: number;
  statusText?: string;
  url?: string;
}
/**
 * Format error message for display in forms or validation contexts
 * Returns field-specific errors as an object
 */
export const formatValidationErrors = (
  response: ErrorResponse
): { [key: string]: string[] } => {
  const fieldErrors: { [key: string]: string[] } = {};

  if (!response?.error?.errors) {
    return fieldErrors;
  }

  const errors = response.error.errors;
  Object.keys(errors).forEach((key) => {
    const fieldMessages = Array.isArray(errors[key])
      ? (errors[key] as string[])
      : [errors[key] as string];
    fieldErrors[key] = fieldMessages;
  });

  return fieldErrors;
};

/**
 * Check if the error response contains validation errors
 */
export const isValidationError = (response: ErrorResponse): boolean => {
  return !!(
    response?.error?.errors && Object.keys(response.error.errors).length > 0
  );
};

/**
 * Get a simple error message for toast notifications
 */
export const getSimpleErrorMessage = (response: ErrorResponse): string => {
  if (!response) {
    return 'Something went wrong! Please try again.';
  }

  // For validation errors, return a general message
  if (isValidationError(response)) {
    return 'Please check the form and fix the validation errors.';
  }

  // For other errors, use the formatted message
  return formatErrorMessage(response);
};
