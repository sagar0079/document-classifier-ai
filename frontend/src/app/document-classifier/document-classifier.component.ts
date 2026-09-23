import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DocumentService, ClassifyResponse } from '../services/document.service';

@Component({
  selector: 'app-document-classifier',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './document-classifier.component.html',
  styleUrl: './document-classifier.component.css'
})
export class DocumentClassifierComponent {
  selectedFile = signal<File | null>(null);
  isDragging = signal(false);
  isLoading = signal(false);
  error = signal<string | null>(null);
  result = signal<ClassifyResponse | null>(null);

  constructor(private documentService: DocumentService) {}

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(): void {
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) this.setFile(file);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.setFile(file);
  }

  private setFile(file: File): void {
    this.selectedFile.set(file);
    this.result.set(null);
    this.error.set(null);
  }

  classify(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.isLoading.set(true);
    this.error.set(null);

    this.documentService.classify(file).subscribe({
      next: (response) => {
        this.result.set(response);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Something went wrong while classifying this document. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  reset(): void {
    this.selectedFile.set(null);
    this.result.set(null);
    this.error.set(null);
  }

  confidencePercent(): number {
    const confidence = this.result()?.classification.confidence ?? 0;
    return Math.round(confidence * 100);
  }
}
