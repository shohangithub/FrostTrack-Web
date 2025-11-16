import { Directive, ElementRef, HostListener, Input, SimpleChanges } from '@angular/core';
import { NgModel } from '@angular/forms';

@Directive({
  selector: '[decimal]',
  standalone: true
})
export class DecimaNumberDirective {
  @Input() floatingPoint: number = 2;
  @Input() control: any;

  // Allow decimal numbers and negative values
  private regex: RegExp = new RegExp(/^\d*\.?\d{0,2}$/g);
  private acceptanceRegex: RegExp = new RegExp(/^\d*\.\d{0,2}/g);
  // Allow key codes for special events. Reflect :
  // Backspace, tab, end, home
  private specialKeys: Array<string> = ['Backspace', 'Tab', 'End', 'Home', 'ArrowLeft', 'ArrowRight', 'Del', 'Delete'];

  constructor(private el: ElementRef) {
  }

  ngAfterViewInit() {
    this.el.nativeElement.style.textAlign = "right";
  }

  ngOnChanges(changes: SimpleChanges) {
    let current: string = this.el.nativeElement.value;
    const position = this.el.nativeElement.selectionStart;
    let currentValue = changes['control'].currentValue;
    if (currentValue) {
      if (!String(currentValue).match(this.regex))
        if (isNaN(parseFloat(currentValue))) {
          this.el.nativeElement.value = 0;
          this.el.nativeElement.dispatchEvent(new Event("change"));
        }
      let arr = String(currentValue).match(this.acceptanceRegex);
      if (arr && arr.length > 0) {
        this.el.nativeElement.value = arr[0];
        this.el.nativeElement.dispatchEvent(new Event("change"));
      }
    } else {
      this.el.nativeElement.value = 0;
      this.el.nativeElement.dispatchEvent(new Event("change"));
    }
  }


  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent) {

    // Allow Backspace, tab, end, and home keys
    if (this.specialKeys.indexOf(event.key) !== -1) {
      return;
    }
    let current: string = this.el.nativeElement.value;
    const position = this.el.nativeElement.selectionStart;
    const next: string = [current.slice(0, position), event.key == 'Decimal' ? '.' : event.key, current.slice(position)].join('');
    if (next && !String(next).match(this.regex)) {
      event.preventDefault();
    }
  }

}
