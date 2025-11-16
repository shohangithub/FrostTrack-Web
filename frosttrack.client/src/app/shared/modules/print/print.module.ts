import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

// Components
import { PrintPreviewComponent } from './components/print-preview/print-preview.component';
import { PrintSettingsComponent } from './components/print-settings/print-settings.component';
import { PrintTemplateComponent } from './components/print-template/print-template.component';
import { PrintHeaderComponent } from './components/print-header/print-header.component';
import { PrintFooterComponent } from './components/print-footer/print-footer.component';

// Services
import { PrintService } from './services/print.service';
import { PrintTemplateService } from './services/print-template.service';
import { PrintConfigurationService } from './services/print-configuration.service';

// Directives
import { PrintDirective } from './directives/print.directive';

// Pipes
import { PrintFormatPipe } from './pipes/print-format.pipe';

@NgModule({
  imports: [
    CommonModule,
    ReactiveFormsModule,
    // Import standalone components
    PrintPreviewComponent,
    PrintSettingsComponent,
    PrintTemplateComponent,
    PrintHeaderComponent,
    PrintFooterComponent,
    PrintDirective,
    PrintFormatPipe,
  ],
  providers: [PrintService, PrintTemplateService, PrintConfigurationService],
  exports: [
    PrintPreviewComponent,
    PrintSettingsComponent,
    PrintTemplateComponent,
    PrintHeaderComponent,
    PrintFooterComponent,
    PrintDirective,
    PrintFormatPipe,
  ],
})
export class PrintModule {}
