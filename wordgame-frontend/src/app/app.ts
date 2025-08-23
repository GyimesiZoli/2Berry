import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {
  mode: 'anagram' | 'wordchain' = 'anagram';
  input1 = '';
  input2 = '';
  result: string[] = [];
  message = '';

  constructor(private http: HttpClient, private cdr: ChangeDetectorRef) { }

  run() {
    this.result = [];
    this.message = '';

    if (this.mode === 'anagram') {
      if (this.input1.length !== 5) {
        this.message = 'Adj meg pontosan 5 betűs szót!';
        this.cdr.detectChanges();
        return;
      }

      this.http.post<string[]>('/anagram', { word: this.input1 })
        .subscribe({
          next: res => {
            this.result = Array.isArray(res) ? res : [];
            if (this.result.length === 0) this.message = 'Nincs találat.';
            this.cdr.detectChanges(); 
          },
          error: err => {
            this.message = (err?.error ?? 'Hiba történt a hívás során.');
            this.cdr.detectChanges();
          }
        });

    } else {
      if (this.input1.length !== 5 || this.input2.length !== 5) {
        this.message = 'Mindkét szónak pontosan 5 betűsnek kell lennie!';
        this.cdr.detectChanges();
        return;
      }

      this.http.post<string[]>('/wordchain', { source: this.input1, target: this.input2 })
        .subscribe({
          next: res => {
            this.result = Array.isArray(res) ? res : [];
            if (this.result.length === 0) this.message = 'Nem található szólánc.';
            this.cdr.detectChanges(); 
          },
          error: err => {
            this.message = (err?.error ?? 'Nem található szólánc.');
            this.cdr.detectChanges();
          }
        });
    }
  }
  onModeChange() {
    this.result = [];
    this.message = '';
  }

}
