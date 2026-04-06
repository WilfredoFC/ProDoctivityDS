import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatRadioModule } from '@angular/material/radio';
import { MatExpansionModule } from '@angular/material/expansion';

import { ConfigurationService } from '../../data/service/configuration.service';
import { SaveConfigurationRequest } from '../../core/models/configuration.model';

@Component({
  selector: 'app-configuration',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatRadioModule,
    MatTabsModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatSnackBarModule,
    MatIconModule,
    MatCardModule,
  ],
  templateUrl: './configuration.html',
  styleUrls: ['./configuration.css']
})
export class ConfigurationComponent implements OnInit {
  private fb = inject(FormBuilder);
  private configService = inject(ConfigurationService);
  private snackBar = inject(MatSnackBar);

  // Formulario principal
  configForm!: FormGroup;

  // Control de visibilidad de campos sensibles
  hideApiKey = true;
  hideApiSecret = true;
  hideBearerToken = true;
  hideCookie = true;

  ngOnInit(): void {
    this.buildForm();
    this.loadConfiguration();
  }

  private buildForm(): void {
    this.configForm = this.fb.group({
      // Credenciales API
      apiCredentials: this.fb.group({
        baseUrl: ['', [Validators.required, Validators.pattern('https?://.+')]],
        apiKey: [''],
        apiSecret: [''],
        bearerToken: [''],
        cookieSessionId: [''],
      }),
      // Opciones de procesamiento
      processingOptions: this.fb.group({
  removeFirstPage: [true],
  onlyIfCriteriaMet: [true],
  updateApi: [false],
  saveOriginalFiles: [false],
  autoRemoveAllSeparators: [false],
  createBackup: [false],
  removePagesEnabled: [false],
  pagesToRemove: ['1'],
  removeMode: ['specific'],
  pageRangeStart: [1],
  pageRangeEnd: [1],
  analyzeAllPages: [false],
  showExtractedText: [false],
}),
      // Reglas de análisis
      analysisRules: this.fb.group({
        criterion1: this.fb.group({
          text: ['SEPARADOR DE DOCUMENTOS'],
          isRegex: [false],
        }),
        criterion2: this.fb.group({
          text: ['DOC-001'],
          isRegex: [false],
        }),
        normalization: this.fb.group({
          isEnabled: [true],
          toUpperCase: [true],
          removeAccents: [true],
          removePunctuation: [true],
          ignoreLineBreaks: [true],
          trimExtraSpaces: [true],
        }),
        evaluationLogic: ['Or'],
      }),
    });
  }

  private loadConfiguration(): void {
    this.configService.getConfiguration().subscribe({
      next: (config) => {
        // Parchear los valores al formulario
        this.configForm.patchValue({
          apiCredentials: {
            baseUrl: config.baseUrl,
            apiKey: config.apiKey, 
            apiSecret: config.apiSecret,
            bearerToken: config.bearerToken,
            cookieSessionId: config.cookieSessionId,
          },
          processingOptions: config.processingOptions,
          analysisRules: config.analysisRules,
        });
      },
      error: (err) => {
        this.snackBar.open('Error al cargar configuración', 'Cerrar', { duration: 3000 });
        console.error(err);
      }
    });
  }

  onSave(): void {
    if (this.configForm.invalid) {
      this.snackBar.open('Por favor completa los campos requeridos', 'Cerrar', { duration: 3000 });
      return;
    }

    const formValue = this.configForm.value;
    const request: SaveConfigurationRequest = {
      apiCredentials: formValue.apiCredentials,
      processingOptions: formValue.processingOptions,
      analysisRules: formValue.analysisRules,
    };

    this.configService.saveConfiguration(request).subscribe({
      next: () => {
        this.snackBar.open('Configuración guardada', 'OK', { duration: 2000 });
      },
      error: (err) => {
        this.snackBar.open('Error al guardar', 'Cerrar', { duration: 3000 });
        console.error(err);
      }
    });
  }

  onTestConnection(): void {
    const credentials = this.configForm.value.apiCredentials;
    if (!credentials.baseUrl) {
      this.snackBar.open('La URL base es requerida', 'Cerrar', { duration: 3000 });
      return;
    }

    this.configService.testConnection(credentials).subscribe({
      next: (success) => {
        const msg = success ? 'Conexión exitosa' : 'Error de conexión';
        const panelClass = success ? ['success-snackbar'] : ['error-snackbar'];
        this.snackBar.open(msg, 'Cerrar', { duration: 3000, panelClass });
      },
      error: () => {
        this.snackBar.open('Error al probar conexión', 'Cerrar', { duration: 3000 });
      }
    });
  }

  onExport(): void {
    this.configService.exportConfiguration().subscribe({
      next: (config) => {
        const blob = new Blob([JSON.stringify(config, null, 2)], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'configuracion.json';
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.snackBar.open('Error al exportar', 'Cerrar', { duration: 3000 });
        console.error(err);
      }
    });
  }

  onImport(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const file = input.files[0];
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const config = JSON.parse(e.target?.result as string);
        this.configService.importConfiguration(config).subscribe({
          next: () => {
            this.snackBar.open('Configuración importada', 'OK', { duration: 2000 });
            this.loadConfiguration(); // recargar
          },
          error: (err) => {
            this.snackBar.open('Error al importar', 'Cerrar', { duration: 3000 });
            console.error(err);
          }
        });
      } catch (ex) {
        this.snackBar.open('Archivo JSON inválido', 'Cerrar', { duration: 3000 });
      }
    };
    reader.readAsText(file);
    // Reset input
    input.value = '';
  }
}