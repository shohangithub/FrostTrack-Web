import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'monthYearText',
  standalone: true,
})
export class MonthYearTextPipe implements PipeTransform {
  transform(value: string): string {
    if (!value) return value;

    return value.replace(/\b(0[1-9]|1[0-2])\/(\d{4})\b/g, (_, month, year) => {
      const date = new Date(+year, +month - 1);
      return date.toLocaleString('en-US', { month: 'short' }) + '/' + year;
    });
  }
}
