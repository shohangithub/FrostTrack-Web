import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'printFormat',
  standalone: true,
})
export class PrintFormatPipe implements PipeTransform {
  transform(value: any, format: string = 'default', ...args: any[]): string {
    if (value === null || value === undefined) {
      return '';
    }

    switch (format.toLowerCase()) {
      case 'currency':
        return this.formatCurrency(value, args[0] || 'USD', args[1] || 2);

      case 'date':
        return this.formatDate(value, args[0] || 'dd/MM/yyyy');

      case 'datetime':
        return this.formatDateTime(value, args[0] || 'dd/MM/yyyy HH:mm');

      case 'number':
        return this.formatNumber(value, args[0] || 2);

      case 'percentage':
        return this.formatPercentage(value, args[0] || 2);

      case 'phone':
        return this.formatPhone(value, args[0] || 'US');

      case 'address':
        return this.formatAddress(value);

      case 'title':
        return this.formatTitle(value);

      case 'upper':
        return String(value).toUpperCase();

      case 'lower':
        return String(value).toLowerCase();

      case 'capitalize':
        return this.capitalize(String(value));

      case 'truncate':
        return this.truncate(String(value), args[0] || 50);

      case 'yesno':
        return this.formatYesNo(value);

      case 'list':
        return this.formatList(value, args[0] || ', ');

      default:
        return String(value);
    }
  }

  private formatCurrency(
    value: number,
    currency: string = 'USD',
    decimals: number = 2
  ): string {
    try {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency,
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals,
      }).format(Number(value));
    } catch {
      return `${currency} ${Number(value).toFixed(decimals)}`;
    }
  }

  private formatDate(
    value: string | Date,
    format: string = 'dd/MM/yyyy'
  ): string {
    try {
      const date = new Date(value);
      if (isNaN(date.getTime())) {
        return String(value);
      }

      const day = date.getDate().toString().padStart(2, '0');
      const month = (date.getMonth() + 1).toString().padStart(2, '0');
      const year = date.getFullYear();
      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      const seconds = date.getSeconds().toString().padStart(2, '0');

      return format
        .replace('dd', day)
        .replace('MM', month)
        .replace('yyyy', year.toString())
        .replace('yy', year.toString().slice(-2))
        .replace('HH', hours)
        .replace('mm', minutes)
        .replace('ss', seconds);
    } catch {
      return String(value);
    }
  }

  private formatDateTime(
    value: string | Date,
    format: string = 'dd/MM/yyyy HH:mm'
  ): string {
    return this.formatDate(value, format);
  }

  private formatNumber(value: number, decimals: number = 2): string {
    try {
      return Number(value).toFixed(decimals);
    } catch {
      return String(value);
    }
  }

  private formatPercentage(value: number, decimals: number = 2): string {
    try {
      return `${(Number(value) * 100).toFixed(decimals)}%`;
    } catch {
      return String(value);
    }
  }

  private formatPhone(value: string, format: string = 'US'): string {
    const cleaned = String(value).replace(/\D/g, '');

    switch (format.toUpperCase()) {
      case 'US':
        if (cleaned.length === 10) {
          return `(${cleaned.slice(0, 3)}) ${cleaned.slice(
            3,
            6
          )}-${cleaned.slice(6)}`;
        }
        if (cleaned.length === 11 && cleaned[0] === '1') {
          return `+1 (${cleaned.slice(1, 4)}) ${cleaned.slice(
            4,
            7
          )}-${cleaned.slice(7)}`;
        }
        break;

      case 'INTERNATIONAL':
        if (cleaned.length > 10) {
          return `+${cleaned.slice(0, -10)} ${cleaned.slice(
            -10,
            -7
          )} ${cleaned.slice(-7, -4)} ${cleaned.slice(-4)}`;
        }
        break;
    }

    return String(value);
  }

  private formatAddress(value: any): string {
    if (typeof value === 'string') {
      return value;
    }

    if (typeof value === 'object' && value !== null) {
      const parts = [
        value.street,
        value.city,
        value.state,
        value.zipCode,
        value.country,
      ].filter((part) => part && part.trim());

      return parts.join(', ');
    }

    return String(value);
  }

  private formatTitle(value: string): string {
    return String(value)
      .toLowerCase()
      .split(' ')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  private capitalize(value: string): string {
    return value.charAt(0).toUpperCase() + value.slice(1).toLowerCase();
  }

  private truncate(value: string, length: number): string {
    if (value.length <= length) {
      return value;
    }
    return value.slice(0, length) + '...';
  }

  private formatYesNo(value: any): string {
    const truthyValues = [true, 'true', '1', 1, 'yes', 'y'];
    return truthyValues.includes(String(value).toLowerCase()) ? 'Yes' : 'No';
  }

  private formatList(value: any, separator: string = ', '): string {
    if (Array.isArray(value)) {
      return value.map((item) => String(item)).join(separator);
    }
    return String(value);
  }
}
