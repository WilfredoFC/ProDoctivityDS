import { Component, signal, OnInit } from '@angular/core';
import { DocumentService } from '../../data/service/document.service';


@Component({
  selector: 'app-document-search',
  templateUrl: './document-search.html',
  styleUrls: ['./document-search.css']
})
export class DocumentSearchComponent implements OnInit {
  documents = signal<any[]>([]);
  totalCount = signal(0);
  loading = signal(false);

  constructor(private documentService: DocumentService) {}

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(page: number = 0): void {
    this.loading.set(true);
    this.documentService.searchDocuments(page, 100).subscribe({
      next: (result) => {
        this.documents.set(result.documents);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error cargando documentos', err);
        this.loading.set(false);
      }
    });
  }
}