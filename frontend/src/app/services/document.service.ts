import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ClassificationResult {
  category: string;
  confidence: number;
  reason: string;
}

export interface ClassifyResponse {
  fileName: string;
  extractedText: string;
  classification: ClassificationResult;
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly apiUrl = `${environment.apiUrl}/api/documents`;

  constructor(private http: HttpClient) {}

  classify(file: File): Observable<ClassifyResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ClassifyResponse>(`${this.apiUrl}/classify`, formData);
  }
}
