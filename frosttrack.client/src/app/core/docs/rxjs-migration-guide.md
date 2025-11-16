# RxJS Utility System - Migration Guide

## Overview

This guide provides a comprehensive approach to migrate from deprecated `toPromise()` patterns to modern RxJS best practices across your entire Angular project.

## 🎯 **Core Philosophy**

- **Use `subscribe()`** for reactive data loading and UI updates
- **Use `firstValueFrom()`** only for sequential async operations that need awaiting
- **Automatic subscription cleanup** with modern Angular patterns
- **Consistent error handling** and loading state management

## 🔧 **Base Components**

### BaseComponent

Provides automatic subscription management and common utilities:

```typescript
export class YourComponent extends BaseComponent implements OnInit {
  rows: any[] = [];
  loadingIndicator = false;

  constructor(private yourService: YourService) {
    super(); // Required for base component
  }

  ngOnInit(): void {
    this.loadData();
  }

  // ✅ GOOD: Reactive data loading
  loadData(): void {
    this.loadData(this.yourService.getData(), {
      loadingState: this, // Automatically manages loadingIndicator
      onSuccess: (data) => {
        this.rows = data;
      },
      errorMessage: "Error loading data",
    });
  }

  // ✅ GOOD: Sequential operations that need awaiting
  async saveData(): Promise<void> {
    const result = await this.executeAsync(this.yourService.save(this.formData), {
      successMessage: "Data saved successfully",
      errorMessage: "Error saving data",
    });

    if (result) {
      // Continue with next operation
    }
  }
}
```

## 📊 **Migration Patterns**

### 1. **Data Loading (Use `subscribe()`)**

```typescript
// ❌ OLD: Deprecated toPromise()
async loadData(): Promise<void> {
  try {
    const response = await this.service.getData().toPromise();
    this.rows = response;
  } catch (error) {
    this.toastr.error('Error loading data');
  }
}

// ✅ NEW: Reactive with BaseComponent
loadData(): void {
  this.loadData(
    this.service.getData(),
    {
      onSuccess: (response) => {
        this.rows = response;
      },
      errorMessage: 'Error loading data'
    }
  );
}
```

### 2. **Paginated Data (Use `subscribe()`)**

```typescript
// ❌ OLD: Manual pagination
async loadPayments(): Promise<void> {
  this.loadingIndicator = true;
  try {
    const paginationQuery = {
      pageIndex: this.currentPage - 1,
      pageSize: this.pageSize,
    };
    const response = await this.service.getWithPagination(paginationQuery).toPromise();
    this.rows = response.data;
    this.totalItems = response.paging.totalData;
  } finally {
    this.loadingIndicator = false;
  }
}

// ✅ NEW: Reactive pagination with BaseComponent
loadPayments(): void {
  const paginationQuery = {
    pageIndex: this.currentPage - 1,
    pageSize: this.pageSize,
  };

  this.loadData(
    this.service.getWithPagination(paginationQuery),
    {
      loadingState: this, // Automatically manages loadingIndicator
      onSuccess: (response) => {
        this.rows = response.data;
        this.totalItems = response.paging.totalData;
      },
      errorMessage: 'Error loading data'
    }
  );
}
```

### 3. **Save Operations (Use `firstValueFrom()`)**

```typescript
// ❌ OLD: Deprecated toPromise()
async savePayment(): Promise<void> {
  try {
    const response = await this.service.create(this.formData).toPromise();
    this.toastr.success('Payment saved successfully');
  } catch (error) {
    this.toastr.error('Error saving payment');
  }
}

// ✅ NEW: firstValueFrom with BaseComponent
async savePayment(): Promise<void> {
  await this.executeAsync(
    this.service.create(this.formData),
    {
      successMessage: 'Payment saved successfully',
      errorMessage: 'Error saving payment',
      onSuccess: (response) => {
        this.paymentId = response.id;
        // Continue with next operation if needed
      }
    }
  );
}
```

### 4. **Route Parameters (Use `subscribe()`)**

```typescript
// ❌ OLD: Manual subscription management
ngOnInit(): void {
  this.route.params.subscribe(params => {
    if (params['id']) {
      this.loadPaymentData(+params['id']);
    }
  });
}

ngOnDestroy(): void {
  // Manual cleanup required
}

// ✅ NEW: Automatic cleanup with BaseComponent
ngOnInit(): void {
  this.subscribeToRoute(
    this.route.params,
    (params) => {
      if (params['id']) {
        this.loadPaymentData(+params['id']);
      }
    }
  );
}

// No ngOnDestroy needed - automatic cleanup
```

### 5. **Form Changes (Use `subscribe()`)**

```typescript
// ❌ OLD: Manual subscription
ngOnInit(): void {
  this.form.valueChanges.subscribe(value => {
    this.calculateTotals(value);
  });
}

// ✅ NEW: Automatic cleanup
ngOnInit(): void {
  this.subscribeToFormChanges(
    this.form.valueChanges,
    (value) => {
      this.calculateTotals(value);
    }
  );
}
```

## 🚀 **Quick Migration Steps**

1. **Extend BaseComponent**:

   ```typescript
   export class YourComponent extends BaseComponent {
     constructor(private service: YourService) {
       super(); // Important!
     }
   }
   ```

2. **Replace `toPromise()` in data loading**:

   ```typescript
   // Replace this pattern
   const data = await this.service.getData().toPromise();

   // With this
   this.loadData(this.service.getData(), {
     onSuccess: (data) => {
       /* handle data */
     },
   });
   ```

3. **Keep `firstValueFrom()` for sequential operations**:

   ```typescript
   // Keep this pattern for saves/deletes
   const result = await this.executeAsync(this.service.save(data));
   ```

4. **Remove manual OnDestroy**:
   ```typescript
   // Remove OnDestroy implementation - BaseComponent handles it
   ```

## 🔍 **Component Checklist**

- [ ] Extends `BaseComponent`
- [ ] Constructor calls `super()`
- [ ] Data loading uses `this.loadData()`
- [ ] Route params use `this.subscribeToRoute()`
- [ ] Form changes use `this.subscribeToFormChanges()`
- [ ] Save operations use `this.executeAsync()`
- [ ] No manual `OnDestroy` implementation needed

## 📁 **File Structure**

```
src/app/core/
├── base/
│   ├── base-component.ts          # Main base component
│   └── paginated-component.ts     # For paginated components
└── services/
    └── rxjs-utility.service.ts    # Additional RxJS utilities
```

## 🔄 **Benefits**

- **Memory Safety**: Automatic subscription cleanup
- **Performance**: Optimized reactive patterns
- **Consistency**: Standardized error handling
- **Maintainability**: Less boilerplate code
- **Future-Proof**: Uses modern Angular patterns

## 📋 **Project-Wide Migration Priority**

1. **High Priority**: Components with many subscriptions
2. **Medium Priority**: Data-heavy components
3. **Low Priority**: Simple components with minimal RxJS usage

Apply this pattern consistently across your entire codebase for the best results!
