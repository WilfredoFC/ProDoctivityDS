import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BatchStateService } from './batch-state.service';
import { DuplicateService } from './duplicate.service';
import { ProcessingService } from './processing.service';

@Injectable({ providedIn: 'root' })
export class BatchProcessorService {
  private state = inject(BatchStateService);
  private duplicateService = inject(DuplicateService);
  private processingService = inject(ProcessingService);
  private snackBar = inject(MatSnackBar);

  private isProcessing = false;

  async processCedulas(fileName: string, cedulas: string[]): Promise<void> {
    if (this.isProcessing) {
      this.snackBar.open('Ya hay un lote en proceso', 'Cerrar', { duration: 2000 });
      return;
    }
    this.isProcessing = true;

    this.state.startProcessing(fileName, cedulas.length);

    let processed = 0;
    let deleted = 0;

    for (let i = 0; i < cedulas.length; i++) {
      // 🔁 Verificar si se solicitó cancelación
      if (this.state.progress().cancelled) {
        this.snackBar.open('Procesamiento cancelado por el usuario', 'Cerrar', { duration: 3000 });
        break;
      }

      const ced = cedulas[i];
      const percent = ((i + 1) / cedulas.length) * 100;
      this.state.updateProgress(i, ced, processed, deleted, percent);

      try {
        const response = await firstValueFrom(
          this.duplicateService.checkByCedula({ cedula: ced })
        );

        if (response.groups && response.groups.length > 0) {
          let groupDeleted = 0;
          for (const group of response.groups) {
            const docsToDelete = group.documents.slice(1);
            for (const doc of docsToDelete) {
              await firstValueFrom(this.processingService.deleteDocument(doc.documentId));
              groupDeleted++;
            }
          }
          deleted += groupDeleted;
          this.snackBar.open(`Cédula ${ced}: ${groupDeleted} duplicados eliminados`, undefined, { duration: 1500 });
        } else {
          this.snackBar.open(`Cédula ${ced}: sin duplicados`, undefined, { duration: 1000 });
        }
        processed++;
        this.state.updateProgress(i, ced, processed, deleted, percent);
      } catch (err: any) {
        this.state.addError(`Cédula ${ced}: ${err.message}`);
        this.snackBar.open(`Error en cédula ${ced}`, 'Cerrar', { duration: 2000 });
      }
    }

    this.state.finishProcessing();
    this.isProcessing = false;
    if (!this.state.progress().cancelled) {
      this.snackBar.open('Procesamiento por lote completado', 'OK', { duration: 3000 });
    }
  }

  cancelProcessing() {
    if (this.isProcessing) {
      this.state.cancel();
      this.snackBar.open('Cancelando proceso...', 'Cerrar', { duration: 2000 });
    }
  }
}