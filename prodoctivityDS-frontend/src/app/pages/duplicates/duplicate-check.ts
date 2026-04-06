import { Component, inject, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { firstValueFrom } from 'rxjs';
import { DuplicateService } from '../../data/service/duplicate.service';
import { ProcessingService } from '../../data/service/processing.service';
import { BatchStateService } from '../../data/service/batch-state.service';
import { BatchProcessorService } from '../../data/service/batch-processor.service';
import { DuplicateCheckResponse, DuplicateGroup } from '../../core/models/duplicate.models';

@Component({
  selector: 'app-duplicate-check',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatExpansionModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatDividerModule
  ],
  templateUrl: './duplicate-check.html',
  styleUrls: ['./duplicate-check.css']
})
export class DuplicateCheckComponent {
  private duplicateService = inject(DuplicateService);
  private processingService = inject(ProcessingService);
  private snackBar = inject(MatSnackBar);
  private cdr = inject(ChangeDetectorRef);
  private ngZone = inject(NgZone);
  private batchState = inject(BatchStateService);
  private batchProcessor = inject(BatchProcessorService);

  // Estado individual
  cedula = '';
  loading = false;
  deleting = false;
  error: string | null = null;
  result: DuplicateCheckResponse | null = null;
  infoMessage: string | null = null;

  // Estado del lote (señal pública)
  batchProgress = this.batchState.progress;
cancelBatchProcessing(): void {
  this.batchProcessor.cancelProcessing();
}
  // ==================== BÚSQUEDA INDIVIDUAL ====================
  async checkDuplicates(): Promise<void> {
    if (!this.cedula.trim()) {
      this.snackBar.open('Ingrese una cédula', 'Cerrar', { duration: 2000 });
      return;
    }

    this.loading = true;
    this.error = null;
    this.infoMessage = null;
    this.result = null;

    try {
      const response = await firstValueFrom(
        this.duplicateService.checkByCedula({ cedula: this.cedula })
      );
      this.ngZone.run(() => {
        this.result = response;
        if (response.groups.length === 0) {
          this.infoMessage = 'No se encontraron duplicados para la cédula ingresada.';
        }
        this.loading = false;
        this.cdr.detectChanges();
      });
    } catch (err: any) {
      this.ngZone.run(() => {
        this.error = 'Error al buscar duplicados: ' + err.message;
        this.loading = false;
        this.cdr.detectChanges();
      });
    }
  }

  // ==================== ELIMINACIÓN DE DUPLICADOS ====================
  async deleteDocument(docId: string): Promise<void> {
    if (this.deleting) return;
    if (!confirm('¿Estás seguro de que deseas eliminar este documento?')) return;

    this.deleting = true;
    try {
      await firstValueFrom(this.processingService.deleteDocument(docId));
      this.snackBar.open('Documento eliminado', 'OK', { duration: 2000 });
      await this.checkDuplicates();
    } catch (err: any) {
      this.snackBar.open('Error al eliminar: ' + err.message, 'Cerrar', { duration: 3000 });
    } finally {
      this.deleting = false;
    }
  }

  async deleteAllDuplicatesInGroup(group: DuplicateGroup): Promise<void> {
    if (this.deleting) return;
    const docsToDelete = group.documents.slice(1);
    if (docsToDelete.length === 0) {
      this.snackBar.open('No hay documentos duplicados para eliminar en este grupo', 'OK', { duration: 2000 });
      return;
    }

    const confirmMessage = `¿Eliminar ${docsToDelete.length} documento(s) duplicado(s) de este grupo? Se conservará "${group.documents[0].name}".`;
    if (!confirm(confirmMessage)) return;

    this.deleting = true;
    let successCount = 0;
    try {
      for (const doc of docsToDelete) {
        await firstValueFrom(this.processingService.deleteDocument(doc.documentId));
        successCount++;
      }
      this.snackBar.open(`${successCount} documento(s) eliminado(s)`, 'OK', { duration: 2000 });
      await this.checkDuplicates();
    } catch (err: any) {
      this.snackBar.open(`Error después de ${successCount} eliminaciones: ${err.message}`, 'Cerrar', { duration: 3000 });
    } finally {
      this.deleting = false;
    }
  }

  async deleteAllDuplicates(): Promise<void> {
    if (this.deleting || !this.result) return;

    const allDocsToDelete = this.result.groups.flatMap(group => group.documents.slice(1));
    if (allDocsToDelete.length === 0) {
      this.snackBar.open('No hay documentos duplicados para eliminar', 'OK', { duration: 2000 });
      return;
    }

    const confirmMessage = `¿Eliminar todos los ${allDocsToDelete.length} documentos duplicados? Se conservará uno por grupo.`;
    if (!confirm(confirmMessage)) return;

    this.deleting = true;
    let successCount = 0;
    try {
      for (const doc of allDocsToDelete) {
        await firstValueFrom(this.processingService.deleteDocument(doc.documentId));
        successCount++;
      }
      this.snackBar.open(`${successCount} documento(s) eliminado(s)`, 'OK', { duration: 2000 });
      await this.checkDuplicates();
    } catch (err: any) {
      this.snackBar.open(`Error después de ${successCount} eliminaciones: ${err.message}`, 'Cerrar', { duration: 3000 });
    } finally {
      this.deleting = false;
    }
  }

  // ==================== PROCESAMIENTO POR LOTE ====================
  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    const fileName = file.name;

    try {
      const content = await this.readFileAsText(file);
      const lines = content.split(/\r?\n/).filter(line => line.trim().length > 0);
      if (lines.length === 0) {
        this.snackBar.open('El archivo no contiene cédulas', 'Cerrar', { duration: 3000 });
        return;
      }
      if (!confirm(`Se procesarán ${lines.length} cédulas. ¿Continuar?`)) return;

      // Iniciar procesamiento a través del servicio
      this.batchProcessor.processCedulas(fileName, lines);
    } catch (err) {
      this.snackBar.open('Error al leer el archivo', 'Cerrar', { duration: 3000 });
    } finally {
      input.value = '';
    }
  }

  clearBatchResults(): void {
    this.batchState.clear();
  }

  private readFileAsText(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e) => resolve(e.target?.result as string);
      reader.onerror = (e) => reject(e);
      reader.readAsText(file);
    });
  }
}