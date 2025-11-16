import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';

export interface ErrorInfo {
  status: number;
  message: string;
  title: string;
  type: 'error' | 'warning' | 'info' | 'success';
}

@Injectable({
  providedIn: 'root',
})
export class ErrorHandlerService {
  constructor(private toastr: ToastrService) {}

  /**
   * Handle errors in components with user-friendly messages
   */
  handleComponentError(error: any, context: string = 'Operation'): void {
    console.error(`Error in ${context}:`, error);

    let errorInfo: ErrorInfo;

    if (error instanceof HttpErrorResponse) {
      errorInfo = this.getHttpErrorInfo(error);
    } else if (error instanceof Error) {
      errorInfo = {
        status: 0,
        message: error.message || 'An unexpected error occurred',
        title: `${context} Failed`,
        type: 'error',
      };
    } else {
      errorInfo = {
        status: 0,
        message: 'An unknown error occurred',
        title: `${context} Failed`,
        type: 'error',
      };
    }

    this.showToast(errorInfo);
  }

  /**
   * Handle specific HTTP errors with custom messages
   */
  handleHttpError(error: HttpErrorResponse, customMessage?: string): void {
    const errorInfo = this.getHttpErrorInfo(error);

    if (customMessage) {
      errorInfo.message = customMessage;
    }

    this.showToast(errorInfo);
  }

  /**
   * Show success message
   */
  showSuccess(message: string, title: string = 'Success'): void {
    this.toastr.success(message, title, {
      timeOut: 4000,
      closeButton: true,
      progressBar: true,
    });
  }

  /**
   * Show warning message
   */
  showWarning(message: string, title: string = 'Warning'): void {
    this.toastr.warning(message, title, {
      timeOut: 6000,
      closeButton: true,
      progressBar: true,
    });
  }

  /**
   * Show info message
   */
  showInfo(message: string, title: string = 'Information'): void {
    this.toastr.info(message, title, {
      timeOut: 5000,
      closeButton: true,
      progressBar: true,
    });
  }

  private getHttpErrorInfo(error: HttpErrorResponse): ErrorInfo {
    switch (error.status) {
      case 400:
        return {
          status: 400,
          message:
            error.error?.message || 'Invalid request. Please check your input.',
          title: 'Bad Request',
          type: 'error',
        };
      case 401:
        return {
          status: 401,
          message: 'Authentication required. Please log in.',
          title: 'Unauthorized',
          type: 'error',
        };
      case 403:
        return {
          status: 403,
          message: 'You do not have permission to perform this action.',
          title: 'Access Forbidden',
          type: 'error',
        };
      case 404:
        return {
          status: 404,
          message: 'The requested resource was not found.',
          title: 'Not Found',
          type: 'error',
        };
      case 409:
        return {
          status: 409,
          message: error.error?.message || 'Conflict with existing data.',
          title: 'Data Conflict',
          type: 'warning',
        };
      case 422:
        return {
          status: 422,
          message: this.formatValidationErrors(error.error),
          title: 'Validation Error',
          type: 'warning',
        };
      case 500:
        return {
          status: 500,
          message: 'Internal server error. Please try again later.',
          title: 'Server Error',
          type: 'error',
        };
      case 0:
        return {
          status: 0,
          message: 'Unable to connect to server. Please check your connection.',
          title: 'Network Error',
          type: 'error',
        };
      default:
        return {
          status: error.status,
          message:
            error.error?.message ||
            error.message ||
            'An unexpected error occurred.',
          title: `Error ${error.status}`,
          type: 'error',
        };
    }
  }

  private formatValidationErrors(errorData: any): string {
    if (errorData?.errors) {
      const errors = Object.values(errorData.errors).flat();
      return errors.join(', ');
    }
    return errorData?.message || 'Validation failed';
  }

  private showToast(errorInfo: ErrorInfo): void {
    switch (errorInfo.type) {
      case 'error':
        this.toastr.error(errorInfo.message, errorInfo.title, {
          timeOut: 8000,
          closeButton: true,
          progressBar: true,
        });
        break;
      case 'warning':
        this.toastr.warning(errorInfo.message, errorInfo.title, {
          timeOut: 6000,
          closeButton: true,
          progressBar: true,
        });
        break;
      case 'info':
        this.toastr.info(errorInfo.message, errorInfo.title, {
          timeOut: 5000,
          closeButton: true,
          progressBar: true,
        });
        break;
      case 'success':
        this.toastr.success(errorInfo.message, errorInfo.title, {
          timeOut: 4000,
          closeButton: true,
          progressBar: true,
        });
        break;
    }
  }
}
