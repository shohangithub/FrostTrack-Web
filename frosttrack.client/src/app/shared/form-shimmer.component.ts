import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { ScremerShimmerModule } from 'scremer-shimmer';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-form-shimmer',
  template: `
  <scremer-shimmer [height]="shimmerHeight+'px'" [width]="'70%'"></scremer-shimmer>
  <scremer-shimmer [height]="shimmerHeight+'px'" [width]="'90%'"></scremer-shimmer>
  <scremer-shimmer [height]="shimmerHeight+'px'" [width]="'50%'"></scremer-shimmer>
  <scremer-shimmer [height]="shimmerHeight+'px'" [width]="'70%'"></scremer-shimmer>
  <scremer-shimmer [height]="shimmerHeight+'px'" [width]="'40%'"></scremer-shimmer>
  `,
  standalone: true,
  imports: [
    CommonModule,
    ScremerShimmerModule
  ],
})
export class FormShimmerComponent implements OnChanges {

  @Input() height: number = 100; // in pixels

  shimmerHeight: number = 0;

  constructor() { }
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['height'].currentValue)
      this.shimmerHeight = this.height / 5;

  }
}
