import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import Swal from 'sweetalert2';
import { SwalConfirm } from 'app/theme-config';
import { MessageHub } from '@config/message-hub';
import { LayoutService } from '@core/service/layout.service';
import { BalanceSheetSetupService } from './balance-sheet-setup.service';
import {
  ENTRY_TYPE_LIST,
  IBalanceSheetEntry,
  IBalanceSheetEntryRequest,
  LINE_ITEM_LIST,
  MONTH_LIST,
  PROFIT_CATEGORY_LIST,
} from './balance-sheet-setup.interfaces';

@Component({
  selector: 'app-balance-sheet-setup',
  templateUrl: './balance-sheet-setup.component.html',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
})
export class BalanceSheetSetupComponent implements OnInit {
  settingsForm: UntypedFormGroup;
  entryForm: UntypedFormGroup;

  entries: IBalanceSheetEntry[] = [];
  editingId: string | null = null;
  isLoading = false;
  isSaving = false;
  isSavingSettings = false;

  entryTypeList = ENTRY_TYPE_LIST;
  lineItemList = LINE_ITEM_LIST;
  profitCategoryList = PROFIT_CATEGORY_LIST;
  monthList = MONTH_LIST;

  constructor(
    private fb: UntypedFormBuilder,
    private setupService: BalanceSheetSetupService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    this.settingsForm = this.fb.group({
      balanceSheetStartDate: [null],
      fiscalYearStartMonth: [7, Validators.required],
    });

    this.entryForm = this.fb.group({
      entryDate: [null, Validators.required],
      entryType: ['OPENING', Validators.required],
      lineItem: [null, Validators.required],
      accountName: ['', [Validators.required, Validators.maxLength(200)]],
      amount: [null, Validators.required],
      profitCategory: [null],
      note: [''],
    });
  }

  ngOnInit(): void {
    this.loadSettings();
    this.loadEntries();
  }

  get isAdjustment(): boolean {
    return this.entryForm.value.entryType === 'ADJUSTMENT';
  }

  /** Adjustments only move asset / liability lines (their other side is profit). */
  get availableLineItems() {
    return this.isAdjustment
      ? this.lineItemList.filter((x) => x.section !== 'Equity')
      : this.lineItemList;
  }

  loadSettings(): void {
    this.setupService.getSettings().subscribe({
      next: (settings) => {
        this.settingsForm.patchValue({
          balanceSheetStartDate: settings.balanceSheetStartDate
            ? settings.balanceSheetStartDate.substring(0, 10)
            : null,
          fiscalYearStartMonth: settings.fiscalYearStartMonth || 7,
        });
        if (!this.entryForm.value.entryDate && settings.balanceSheetStartDate) {
          this.entryForm.patchValue({ entryDate: settings.balanceSheetStartDate.substring(0, 10) });
        }
      },
    });
  }

  saveSettings(): void {
    if (this.settingsForm.invalid) return;
    this.isSavingSettings = true;
    this.setupService
      .updateSettings({
        balanceSheetStartDate: this.settingsForm.value.balanceSheetStartDate || null,
        fiscalYearStartMonth: Number(this.settingsForm.value.fiscalYearStartMonth),
      })
      .subscribe({
        next: () => (this.isSavingSettings = false),
        error: () => (this.isSavingSettings = false),
      });
  }

  loadEntries(): void {
    this.isLoading = true;
    this.setupService.getAll().subscribe({
      next: (entries) => {
        this.entries = entries;
        this.isLoading = false;
      },
      error: () => (this.isLoading = false),
    });
  }

  onEntryTypeChange(): void {
    if (!this.isAdjustment) {
      this.entryForm.patchValue({ profitCategory: null });
    }
    const line = this.entryForm.value.lineItem;
    if (line && !this.availableLineItems.some((x) => x.id === line)) {
      this.entryForm.patchValue({ lineItem: null });
    }
  }

  saveEntry(): void {
    if (this.entryForm.invalid) {
      this.entryForm.markAllAsTouched();
      return;
    }
    const value = this.entryForm.value;
    const payload: IBalanceSheetEntryRequest = {
      entryDate: value.entryDate,
      entryType: value.entryType,
      lineItem: value.lineItem,
      accountName: value.accountName,
      amount: Number(value.amount),
      profitCategory: this.isAdjustment ? value.profitCategory : null,
      note: value.note,
    };

    this.isSaving = true;
    const request$ = this.editingId
      ? this.setupService.update(this.editingId, payload)
      : this.setupService.create(payload);

    request$.subscribe({
      next: () => {
        this.isSaving = false;
        this.resetEntryForm();
        this.loadEntries();
      },
      error: () => (this.isSaving = false),
    });
  }

  editEntry(entry: IBalanceSheetEntry): void {
    this.editingId = entry.id;
    this.entryForm.setValue({
      entryDate: entry.entryDate.substring(0, 10),
      entryType: entry.entryType,
      lineItem: entry.lineItem,
      accountName: entry.accountName,
      amount: entry.amount,
      profitCategory: entry.profitCategory ?? null,
      note: entry.note ?? '',
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  deleteEntry(entry: IBalanceSheetEntry): void {
    Swal.fire({
      title: MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.setupService.remove(entry.id).subscribe({
          next: () => {
            if (this.editingId === entry.id) this.resetEntryForm();
            this.loadEntries();
          },
        });
      }
    });
  }

  resetEntryForm(): void {
    this.editingId = null;
    const startDate = this.settingsForm.value.balanceSheetStartDate;
    this.entryForm.reset({
      entryDate: startDate || null,
      entryType: 'OPENING',
      lineItem: null,
      accountName: '',
      amount: null,
      profitCategory: null,
      note: '',
    });
  }

  lineItemName(id: string): string {
    return this.lineItemList.find((x) => x.id === id)?.value ?? id;
  }

  lineItemSection(id: string): string {
    return this.lineItemList.find((x) => x.id === id)?.section ?? '';
  }

  entryTypeName(id: string): string {
    return id === 'OPENING' ? 'Opening' : 'Adjustment';
  }

  profitCategoryName(id?: string | null): string {
    return id ? this.profitCategoryList.find((x) => x.id === id)?.value ?? id : '-';
  }
}
