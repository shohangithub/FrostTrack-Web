export interface IBalanceSheetEntry {
  id: string;
  code: string;
  entryDate: string;
  entryType: string;
  lineItem: string;
  accountName: string;
  amount: number;
  profitCategory?: string | null;
  note?: string | null;
}

export interface IBalanceSheetEntryRequest {
  entryDate: string;
  entryType: string;
  lineItem: string;
  accountName: string;
  amount: number;
  profitCategory?: string | null;
  note?: string | null;
}

export interface IBalanceSheetSettings {
  balanceSheetStartDate?: string | null;
  fiscalYearStartMonth: number;
}

export const ENTRY_TYPE_LIST = [
  { id: 'OPENING', value: 'Opening balance / প্রারম্ভিক জের' },
  { id: 'ADJUSTMENT', value: 'Year-end adjustment / সমন্বয়' },
];

export const LINE_ITEM_LIST = [
  { id: 'ADVANCE_DEPOSIT', value: 'Advance, Deposit & Pre-payments', section: 'Asset' },
  { id: 'OTHER_ASSET', value: 'Other Assets', section: 'Asset' },
  { id: 'SUNDRY_CREDITORS', value: 'Sundry Creditors & Loans', section: 'Liability' },
  { id: 'PROVISION', value: 'Provisions & Accrued Expenses', section: 'Liability' },
  { id: 'OTHER_LIABILITY', value: 'Other Liabilities', section: 'Liability' },
  { id: 'SHARE_CAPITAL', value: 'Share Capital', section: 'Equity' },
  { id: 'RETAINED_EARNINGS', value: 'Retained Earnings b/f', section: 'Equity' },
];

export const PROFIT_CATEGORY_LIST = [
  { id: 'EXPENSE_DIRECT', value: 'Direct expense' },
  { id: 'EXPENSE_ADMIN', value: 'Administrative expense' },
  { id: 'EXPENSE_FINANCE', value: 'Finance expense' },
  { id: 'TAX', value: 'Income tax' },
  { id: 'INCOME', value: 'Income' },
];

export const MONTH_LIST = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
].map((name, i) => ({ id: i + 1, value: name }));
