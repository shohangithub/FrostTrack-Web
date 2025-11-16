export const COMMON_STATUS_LIST = [
  { id: 'true', value: 'Active' },
  { id: 'false', value: 'Inactive' },
];

export const SYSTEM_ROLES = [
  { id: 'SUPERADMIN', value: 'Super Admin' },
  { id: 'ADMIN', value: 'Admin' },
  { id: 'MANAGER', value: 'Manager' },
  { id: 'SELLER', value: 'Seller' },
  { id: 'STANDARD', value: 'Standard' },
];

export const SALES_TYPES = {
  RETAIL: 'RETAIL',
  WHOLESALE: 'WHOLESALE',
};

export const DEFAULT_CURRENCY_LIST = [
  { id: 'BDT', value: 'Bangladeshi Taka', symbol: '৳' },
  { id: 'USD', value: 'US Dollar', symbol: '$' },
  { id: 'AUD', value: 'Australian Dollar', symbol: 'A$' },
  { id: 'EUR', value: 'Euro', symbol: '€' },
  { id: 'JPY', value: 'Japanese Yen', symbol: '¥' },
];

export const DEFAULT_PAYMENT_METHOD_TYPE = [
  {
    value: 'Cash',
    text: 'Cash',
    icon: 'fa fa-money-bill-wave',
    colorClass: 'text-success',
    description: 'Physical cash payments',
  },
  {
    value: 'MobileWallet',
    text: 'Mobile Wallet',
    icon: 'fa fa-mobile-alt',
    colorClass: 'text-info',
    description: 'Mobile wallet payments',
  },
  {
    value: 'Card',
    text: 'Credit/Debit Card',
    icon: 'fa fa-credit-card',
    colorClass: 'text-warning',
    description: 'Credit and debit cards',
  },
  {
    value: 'Bank',
    text: 'Bank Transfer',
    icon: 'fa fa-university',
    colorClass: 'text-primary',
    description: 'Bank account transfers',
  },
];

export const PAYMENT_METHOD_ICONS = [
  {
    value: 'fa fa-money-bill-wave',
    text: 'Cash/Money Bill',
    icon: 'fa fa-money-bill-wave',
    category: 'Cash',
    description: 'Physical cash payments',
  },
  {
    value: 'fa fa-coins',
    text: 'Coins',
    icon: 'fa fa-coins',
    category: 'Cash',
    description: 'Coin payments',
  },
  {
    value: 'fa fa-university',
    text: 'Bank/University',
    icon: 'fa fa-university',
    category: 'Bank',
    description: 'Bank transfers and institutional payments',
  },
  {
    value: 'fa fa-building-columns',
    text: 'Bank Building',
    icon: 'fa fa-building-columns',
    category: 'Bank',
    description: 'Banking institution',
  },
  {
    value: 'fa fa-credit-card',
    text: 'Credit Card',
    icon: 'fa fa-credit-card',
    category: 'Card',
    description: 'Credit and debit cards',
  },
  {
    value: 'fa fa-credit-card-alt',
    text: 'Credit Card Alt',
    icon: 'fa fa-credit-card-alt',
    category: 'Card',
    description: 'Alternative credit card style',
  },
  {
    value: 'fa fa-mobile-alt',
    text: 'Mobile Phone',
    icon: 'fa fa-mobile-alt',
    category: 'Digital',
    description: 'Mobile payments and wallets',
  },
  {
    value: 'fa fa-mobile',
    text: 'Mobile',
    icon: 'fa fa-mobile',
    category: 'Digital',
    description: 'Mobile device payments',
  },
  {
    value: 'fa fa-wallet',
    text: 'Wallet',
    icon: 'fa fa-wallet',
    category: 'Digital',
    description: 'Digital wallets',
  },
  {
    value: 'fa fa-globe',
    text: 'Globe/Online',
    icon: 'fa fa-globe',
    category: 'Digital',
    description: 'Online payment gateways',
  },
  {
    value: 'fa fa-money-check',
    text: 'Money Check',
    icon: 'fa fa-money-check',
    category: 'Bank',
    description: 'Check payments',
  },
  {
    value: 'fa fa-money-check-alt',
    text: 'Money Check Alt',
    icon: 'fa fa-money-check-alt',
    category: 'Bank',
    description: 'Alternative check style',
  },
  {
    value: 'fa fa-exchange-alt',
    text: 'Exchange/Transfer',
    icon: 'fa fa-exchange-alt',
    category: 'Bank',
    description: 'Money transfers and exchanges',
  },
  {
    value: 'fa fa-arrow-right',
    text: 'Arrow Right',
    icon: 'fa fa-arrow-right',
    category: 'Bank',
    description: 'Transfer direction',
  },
  {
    value: 'fa fa-qrcode',
    text: 'QR Code',
    icon: 'fa fa-qrcode',
    category: 'Digital',
    description: 'QR code payments',
  },
  {
    value: 'fa fa-barcode',
    text: 'Barcode',
    icon: 'fa fa-barcode',
    category: 'Digital',
    description: 'Barcode payments',
  },
  {
    value: 'fa fa-paypal',
    text: 'PayPal',
    icon: 'fa fa-paypal',
    category: 'Digital',
    description: 'PayPal payments',
  },
  {
    value: 'fa fa-apple-pay',
    text: 'Apple Pay',
    icon: 'fa fa-apple-pay',
    category: 'Digital',
    description: 'Apple Pay payments',
  },
  {
    value: 'fa fa-google-pay',
    text: 'Google Pay',
    icon: 'fa fa-google-pay',
    category: 'Digital',
    description: 'Google Pay payments',
  },
  {
    value: 'fa fa-cc-visa',
    text: 'Visa Card',
    icon: 'fa fa-cc-visa',
    category: 'Card',
    description: 'Visa credit cards',
  },
  {
    value: 'fa fa-cc-mastercard',
    text: 'MasterCard',
    icon: 'fa fa-cc-mastercard',
    category: 'Card',
    description: 'MasterCard credit cards',
  },
  {
    value: 'fa fa-cc-amex',
    text: 'American Express',
    icon: 'fa fa-cc-amex',
    category: 'Card',
    description: 'American Express cards',
  },
  {
    value: 'fa fa-handshake',
    text: 'Handshake',
    icon: 'fa fa-handshake',
    category: 'Cash',
    description: 'Direct payments and agreements',
  },
  {
    value: 'fa fa-receipt',
    text: 'Receipt',
    icon: 'fa fa-receipt',
    category: 'Cash',
    description: 'Payment receipts',
  },
];
