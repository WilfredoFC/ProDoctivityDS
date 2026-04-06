import { Injectable, signal } from '@angular/core';

export interface BatchProgress {
  processing: boolean;
  fileName: string;
  total: number;
  currentIndex: number;
  currentCedula: string;
  processedCount: number;
  deletedCount: number;
  errors: string[];
  progressPercent: number;
  cancelled: boolean; // Nuevo flag
}

@Injectable({ providedIn: 'root' })
export class BatchStateService {
  public progress = signal<BatchProgress>({
    processing: false,
    fileName: '',
    total: 0,
    currentIndex: 0,
    currentCedula: '',
    processedCount: 0,
    deletedCount: 0,
    errors: [],
    progressPercent: 0,
    cancelled: false
  });

  startProcessing(fileName: string, total: number) {
    this.progress.update(s => ({
      ...s,
      processing: true,
      fileName,
      total,
      currentIndex: 0,
      currentCedula: '',
      processedCount: 0,
      deletedCount: 0,
      errors: [],
      progressPercent: 0,
      cancelled: false  // Reiniciamos flag al empezar
    }));
  }

  updateProgress(index: number, cedula: string, processed: number, deleted: number, percent: number) {
    this.progress.update(s => ({
      ...s,
      currentIndex: index,
      currentCedula: cedula,
      processedCount: processed,
      deletedCount: deleted,
      progressPercent: percent
    }));
  }

  addError(error: string) {
    this.progress.update(s => ({
      ...s,
      errors: [...s.errors, error]
    }));
  }

  finishProcessing() {
    this.progress.update(s => ({
      ...s,
      processing: false
    }));
  }

  cancel() {
    this.progress.update(s => ({ ...s, cancelled: true }));
  }

  clear() {
    this.progress.set({
      processing: false,
      fileName: '',
      total: 0,
      currentIndex: 0,
      currentCedula: '',
      processedCount: 0,
      deletedCount: 0,
      errors: [],
      progressPercent: 0,
      cancelled: false
    });
  }
}