import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-print-settings',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="print-settings">
      <h4>Print Settings</h4>
      <p>Print settings component - to be implemented</p>
    </div>
  `,
})
export class PrintSettingsComponent {
  @Input() settings: any;
  @Output() settingsChange = new EventEmitter<any>();
}
