import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-print-template',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="print-template">
      <h4>Print Template</h4>
      <p>Print template component - to be implemented</p>
    </div>
  `,
})
export class PrintTemplateComponent {
  @Input() template: any;
}
