import { JsonPipe } from '@angular/common';
import {
  Component,
  Input,
  SimpleChanges,
  inject,
} from '@angular/core';
import {
  ControlValueAccessor,
  FormsModule,
  NG_VALUE_ACCESSOR,
} from '@angular/forms';
import {
  NgbCalendar,
  NgbDate,
  NgbDateParserFormatter,
  NgbDatepickerModule,
} from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'date-range-picker',
  templateUrl: './date-range-picker.component.html',
  standalone: true,
  imports: [NgbDatepickerModule, FormsModule, JsonPipe],
  styles: `
		.dp-hidden {
			width: 0;
			margin: 0;
			border: none;
			padding: 0;
		}
		.custom-day {
			text-align: center;
			padding: 0.185rem 0.25rem;
			display: inline-block;
			height: 2rem;
			width: 2rem;
		}
		.custom-day.focused {
			background-color: #e6e6e6;
		}
		.custom-day.range,
		.custom-day:hover {
			background-color: rgb(2, 117, 216);
			color: white;
		}
		.custom-day.faded {
			background-color: rgba(2, 117, 216, 0.5);
		}
	`,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      multi: true,
      useExisting: DateRangePickerComponent,
    },
  ],
})
export class DateRangePickerComponent implements ControlValueAccessor {
  @Input() data?: DateRangeModel;

  calendar = inject(NgbCalendar);
  formatter = inject(NgbDateParserFormatter);

  hoveredDate: NgbDate | null = null;
  fromDate: NgbDate | null = this.calendar.getToday();
  toDate: NgbDate | null = this.calendar.getNext(
    this.calendar.getToday(),
    'd',
    0
  );

  ngOnChanges(changes: SimpleChanges) {
    if (changes['data'].currentValue) {
      const input = changes['data'].currentValue;
      if (input) {
        this.fromDate = input.from;
        this.toDate = input.to;
      }
    }
  }

  ngAfterViewInit(){
    if(!this.data){
      this.data = { from: this.fromDate, to: this.toDate };
      this.onChange(this.data);
    }
  }

  onChange = (range: DateRangeModel) => {};
  onTouched = () => {};

  touched = false;

  disabled = false;

  onDateSelection(date: NgbDate) {
    this.markAsTouched();
    if (!this.fromDate && !this.toDate) {
      this.fromDate = date;
    } else if (
      this.fromDate &&
      !this.toDate &&
      date &&
      date.after(this.fromDate)
    ) {
      this.toDate = date;
    } else {
      this.toDate = null;
      this.fromDate = date;
    }
    this.data = { from: this.fromDate, to: this.toDate };
    this.onChange(this.data);
  }

  isHovered(date: NgbDate) {
    return (
      this.fromDate &&
      !this.toDate &&
      this.hoveredDate &&
      date.after(this.fromDate) &&
      date.before(this.hoveredDate)
    );
  }

  isInside(date: NgbDate) {
    return this.toDate && date.after(this.fromDate) && date.before(this.toDate);
  }

  isRange(date: NgbDate) {
    return (
      date.equals(this.fromDate) ||
      (this.toDate && date.equals(this.toDate)) ||
      this.isInside(date) ||
      this.isHovered(date)
    );
  }

  validateInput(currentValue: NgbDate | null, input: string): NgbDate | null {
    const parsed = this.formatter.parse(input);
    return parsed && this.calendar.isValid(NgbDate.from(parsed))
      ? NgbDate.from(parsed)
      : currentValue;
  }

  writeValue(range: DateRangeModel) {
    this.data = range;
  }

  registerOnChange(onChange: any) {
    this.onChange = onChange;
  }

  registerOnTouched(onTouched: any) {
    this.onTouched = onTouched;
  }

  markAsTouched() {
    if (!this.touched) {
      this.onTouched();
      this.touched = true;
    }
  }

  setDisabledState(disabled: boolean) {
    this.disabled = disabled;
  }
}

export interface DateRangeModel {
  from: NgbDate | undefined | null;
  to: NgbDate | undefined | null;
}
