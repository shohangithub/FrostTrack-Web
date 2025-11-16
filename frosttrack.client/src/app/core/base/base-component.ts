import { inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, firstValueFrom } from 'rxjs';

/**
 * Base component class providing automatic subscription management and common utilities
 * Uses Angular 16+ DestroyRef for automatic cleanup - no need for manual OnDestroy implementation
 */
export abstract class BaseComponent {
  protected readonly destroyRef = inject(DestroyRef);

  /**
   * Automatically manages subscription lifecycle - no manual unsubscribe needed
   */
  protected takeUntilDestroyed() {
    return takeUntilDestroyed(this.destroyRef);
  }

  /**
   * Reactive data loader with automatic loading state management
   * Since services now handle error/success messages automatically, this focuses only on data loading
   */
  protected loadData<T>(
    source$: Observable<T>,
    options: {
      onSuccess?: (data: T) => void;
      onError?: (error: any) => void;
      loadingState?: { loadingIndicator?: boolean };
    } = {}
  ): void {
    const { onSuccess, onError, loadingState } = options;

    if (loadingState && 'loadingIndicator' in loadingState) {
      (loadingState as any).loadingIndicator = true;
    }

    source$.pipe(this.takeUntilDestroyed()).subscribe({
      next: (data) => {
        if (loadingState && 'loadingIndicator' in loadingState) {
          (loadingState as any).loadingIndicator = false;
        }
        onSuccess?.(data as T);
      },
      error: (error) => {
        if (loadingState && 'loadingIndicator' in loadingState) {
          (loadingState as any).loadingIndicator = false;
        }
        console.error('Data loading error:', error);
        onError?.(error);
      },
    });
  }

  /**
   * For operations that need to be awaited (navigation, conditional logic, etc.)
   * Services now handle success/error messages automatically
   */
  protected async executeAsync<T>(
    source$: Observable<T>,
    options: {
      onSuccess?: (data: T) => void;
      onError?: (error: any) => void;
    } = {}
  ): Promise<T | null> {
    const { onSuccess, onError } = options;

    try {
      const result = await firstValueFrom(source$);
      onSuccess?.(result);
      return result;
    } catch (error) {
      console.error('Async operation error:', error);
      onError?.(error);
      return null;
    }
  }

  /**
   * Route parameter subscription with automatic cleanup
   */
  protected subscribeToRoute<T>(
    source$: Observable<T>,
    callback: (params: T) => void
  ): void {
    source$.pipe(this.takeUntilDestroyed()).subscribe({
      next: (params) => callback(params as T),
    });
  }

  /**
   * Form changes subscription with automatic cleanup
   */
  protected subscribeToFormChanges<T>(
    source$: Observable<T>,
    callback: (value: T) => void
  ): void {
    source$.pipe(this.takeUntilDestroyed()).subscribe({
      next: (value) => callback(value as T),
    });
  }
}
