import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Document } from '../../core/models/document.model';

export interface SearchResponse {
  documents: Document[];
  totalCount: number;
  currentPage: number;
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);
  private apiUrl = '/api/documents';

  getDocumentIdentityNumber(documentId: string): Observable<{ documentId: string; identityNumber: string | null }> {
  return this.http.get<{ documentId: string; identityNumber: string | null }>(`${this.apiUrl}/${documentId}/matricula`);
}
  // Mapeo para documentos del POST (con prefijo $)
  private mapPostDocument(doc: any): Document {
  return {
    documentId: doc.$documentId,
    name: doc.$name,
    documentTypeName: doc.$documentTypeName,
    documentTypeId: doc.$documentTypeId,
    createdAt: doc.$createdAt,
    data: doc.data // Si existe, aunque en esta respuesta no viene
  };
}

  // Mapeo para documentos del GET (sin prefijo)
  private mapGetDocument(doc: any): Document {
    return {
      documentId: doc.documentId,
      name: doc.name,
      documentTypeName: doc.documentTypeName,
      documentTypeId: doc.documentTypeId,
      data: doc.data,
      createdAt: doc.createdAt
    };
  }

  /**
   * Obtiene todos los documentos sin filtros (GET)
   */
  getAllDocuments(page: number, pageSize: number): Observable<SearchResponse> {
  const params = new HttpParams()
    .set('page', page.toString())
    .set('rowsPerPage', pageSize.toString());

  return this.http.get<any>(this.apiUrl, { params }).pipe(
    map(response => {
      const documents = (response.documents || []).map((d: any) => this.mapGetDocument(d));
      const totalCount = response.totalCount || response.total || 0; // Intenta ambas
      return {
        documents,
        totalCount,
        currentPage: page
      };
    })
  );
}

  /**
   * Busca documentos con filtros (POST)
   */
  searchDocumentsWithFilters(
  documentTypeIds: string[] | undefined,
  query: string | undefined,
  page: number,
  pageSize: number
): Observable<SearchResponse> {
  let params = new HttpParams()
    .set('page', page.toString())
    .set('rowsPerPage', pageSize.toString());

  if (documentTypeIds && documentTypeIds.length > 0) {
    params = params.set('documentTypeIds', documentTypeIds.join(','));
  }
  if (query) {
    params = params.set('query', query);
  }

  return this.http.post<any>(this.apiUrl, null, { params }).pipe(
    map(response => {
      // ✅ La respuesta tiene 'documents' y 'totalCount'
      const documents = (response.documents || []).map((d: any) => this.mapPostDocument(d));
      const totalCount = response.totalCount || 0;
      return {
        documents,
        totalCount,
        currentPage: page
      };
    })
  );
}
/**
 * Obtiene un documento por su ID (incluye metadatos en 'data')
 */
getDocumentById(documentId: string): Observable<Document> {
  return this.http.get<any>(`${this.apiUrl}/${documentId}`).pipe(
    map(response => {
      // La respuesta puede venir como { document: ... } o directamente el documento
      const doc = response.document || response;
      // Usamos mapGetDocument porque el GET individual devuelve estructura sin prefijos $
      return this.mapGetDocument(doc);
    })
  );
}

  /**
   * Método unificado (usado por el componente)
   */
  searchDocuments(
    page: number,
    pageSize: number,
    name?: string,
    documentTypeIds?: string[]
  ): Observable<SearchResponse> {
    if (name || (documentTypeIds && documentTypeIds.length > 0)) {
      return this.searchDocumentsWithFilters(documentTypeIds, name, page, pageSize);
    } else {
      return this.getAllDocuments(page, pageSize);
    }
  }
}