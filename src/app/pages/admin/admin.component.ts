import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';

interface Block { text: string; }

interface PostForm {
  id: string;
  img: string;
  date_en: string;
  date_ar: string;
  title_en: string;
  title_ar: string;
  blocks_en: Block[];
  blocks_ar: Block[];
}

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent {
  private http   = inject(HttpClient);
  private route  = inject(ActivatedRoute);

  tab: 'news' | 'articles' = 'news';
  status = signal<'idle' | 'saving' | 'done' | 'error'>('idle');

  form: PostForm = this.emptyForm();

  showTabs = true;

  constructor() {
    this.route.queryParams.subscribe(p => {
      if (p['type'] === 'articles') { this.tab = 'articles'; this.showTabs = false; }
      else if (p['type'] === 'news') { this.tab = 'news';     this.showTabs = false; }
      else { this.showTabs = true; }
    });
  }

  emptyForm(): PostForm {
    return {
      id: '', img: '', date_en: '', date_ar: '',
      title_en: '', title_ar: '',
      blocks_en: [{ text: '' }],
      blocks_ar: [{ text: '' }]
    };
  }

  addBlock() {
    this.form.blocks_en.push({ text: '' });
    this.form.blocks_ar.push({ text: '' });
  }

  removeBlock(i: number) {
    this.form.blocks_en.splice(i, 1);
    this.form.blocks_ar.splice(i, 1);
  }

  submit() {
    this.status.set('saving');
    const endpoint = `/api/${this.tab}`;
    this.http.post(endpoint, this.form).subscribe({
      next: () => { this.status.set('done'); this.form = this.emptyForm(); },
      error: () => this.status.set('error')
    });
  }
}
