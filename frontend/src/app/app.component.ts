import { Component } from '@angular/core';
import { DocumentClassifierComponent } from './document-classifier/document-classifier.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [DocumentClassifierComponent],
  templateUrl: './app.component.html'
})
export class AppComponent {}
