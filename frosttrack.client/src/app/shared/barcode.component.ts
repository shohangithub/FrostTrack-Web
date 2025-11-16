import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { NgxBarcodeModule } from 'ngx-ivy-barcode';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-barcode',
  styles: `
    .article {
        min-height: 65px;
        max-height: 100px;
        float: left !important;
        writing-mode: tb-rl;
        line-height: 0;
        font-weight: 700;
        transform: rotate(180deg);
    }
  `,
  template: `
    @if (isShow) {
    <div class="barcode-print-body">
      @for (item of barcodeList; track item) {
      <div
        class="barcode-container"
        style="
        float: left; margin: 0px; padding: 0px; overflow: hidden; border-top: 1px solid rgb(204, 204, 204); border-right: 1px solid rgb(204, 204, 204); border-bottom: none; border-left: 1px solid rgb(204, 204, 204); border-image: initial; box-sizing: border-box; width: 1.48in; height: 1in;
        "
      >
        <div
          style="text-align: center; margin: 0px; padding: 0px; width: 1.48in; height: 1in;"
        >
          <span
            class="article"
            style="font-size: 12px; margin-top: 0px; margin-right: 0px; margin-bottom: 0px; margin-left: 15px !important;"
          >
            {{ article }}
          </span>
          <p
            style="font-size: 10px; margin: 0px 0px 2px 1px; padding: 2px 0px 0px; font-weight: bolder; text-align: center; line-height: 1;"
          >
            {{ name }}
          </p>
          <ngx-ivy-barcode
            [bc-value]="code"
            [bc-height]="height"
            [bc-width]="width"
            [bc-display-value]="false"
            [bc-format]="'CODE128'"
            [bc-font-size]="12"
          ></ngx-ivy-barcode>
          <p
            style="margin: -3px 0px 0px; font-size: 12px; text-align: center; font-weight: 900;"
          >
            {{ code }}
          </p>
          <p
            style="margin: -5px 0px 0px; text-align: center; font-size: 12px; font-weight: bolder;"
          >
          Taka: {{ price }}
          </p>
        </div>
      </div>
      } 
    </div>
    }
  `,
  standalone: true,
  imports: [CommonModule, NgxBarcodeModule],
})
export class BarcodeComponent implements OnChanges {
  @Input() height: number = 20; // in inch
  @Input() width: number = 1; // in inch
  @Input() code: string = 'P-0001';
  @Input() name: string = 'Product One';
  @Input() quantity: number = 1;
  @Input() price: number = 15;
  @Input() article: string = 'Artile';
  @Input() isShow: boolean = true;

  shimmerHeight: number = 0;
  value: any;

  constructor() {}
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['height']?.currentValue) this.shimmerHeight = this.height / 5;
    if (changes['quantity']?.currentValue) {
    this.counter(changes['quantity']?.currentValue)
    }
  }
  barcodeList: number[] = [];
  counter(quantity: number) {
    this.barcodeList.length = 0;
    for (let i = 0; i < quantity; i++) {
      this.barcodeList.push(i);
    }
  }
  
}
