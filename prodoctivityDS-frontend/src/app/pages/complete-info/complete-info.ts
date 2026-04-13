import { Component, inject, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DocumentService, DocumentsByCedulaResponse, DocumentForCompletion } from '../../data/service/document.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-complete-info',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './complete-info.html',
  styleUrls: ['./complete-info.css']
})
export class CompleteInfoComponent {
  private documentService = inject(DocumentService);
  private snackBar = inject(MatSnackBar);
  private cdr = inject(ChangeDetectorRef);
  private ngZone = inject(NgZone);

  cedula = '';
  loading = false;
  completing = false;
  result: DocumentsByCedulaResponse | null = null;
  error: string | null = null;

  async search(): Promise<void> {
    if (!this.cedula.trim()) {
      this.snackBar.open('Ingrese una cédula', 'Cerrar', { duration: 2000 });
      return;
    }

    this.loading = true;
    this.error = null;
    this.result = null;

    try {
      const response = await firstValueFrom(this.documentService.getDocumentsByCedula(this.cedula));
      this.ngZone.run(() => {
        this.result = response;
        this.loading = false;
        this.cdr.detectChanges();
      });
    } catch (err: any) {
      this.ngZone.run(() => {
        this.error = err.error?.message || err.message;
        this.loading = false;
        this.cdr.detectChanges();
      });
    }
  }

  async completeInfo(): Promise<void> {
    if (this.completing) return;
    if (!this.result || this.result.documentsWithoutCedulaCount === 0) {
      this.snackBar.open('No hay documentos sin cédula para completar', 'Cerrar', { duration: 2000 });
      return;
    }

    const confirmMsg = `Se actualizarán ${this.result.documentsWithoutCedulaCount} documento(s) que no tienen cédula. ¿Continuar?`;
    if (!confirm(confirmMsg)) return;

    this.completing = true;
    try {
      const response = await firstValueFrom(this.documentService.completeMissingDocuments(this.cedula));
      this.ngZone.run(() => {
        this.snackBar.open(`✅ ${response.documentsWithoutCedulaUpdated} documento(s) actualizados.`, 'OK', { duration: 4000 });
        // Recargar la lista actualizada
        this.search();
        this.completing = false;
        this.cdr.detectChanges();
      });
    } catch (err: any) {
      this.ngZone.run(() => {
        this.snackBar.open('Error: ' + (err.error?.message || err.message), 'Cerrar', { duration: 5000 });
        this.completing = false;
        this.cdr.detectChanges();
      });
    }
  }

  trackByDocumentId(index: number, doc: DocumentForCompletion): string {
    return doc.documentId;
  }
}