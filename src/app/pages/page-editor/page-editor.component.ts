import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { concatMap, from } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { Post, PostsService, imageSrc } from '../../services/posts.service';

interface Block { text: string; }

interface PageForm {
  id: string;
  images: string[];
  title_en: string;
  title_ar: string;
  blocks_en: Block[];
  blocks_ar: Block[];
}

@Component({
  selector: 'app-page-editor',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './page-editor.component.html',
  styleUrl: '../admin/admin.component.scss'
})
export class PageEditorComponent {
  private api = inject(ApiService);
  posts = inject(PostsService);

  editingId: string | null = null;
  status = signal<'idle' | 'saving' | 'done' | 'error'>('idle');
  errorMessage = signal('');
  form: PageForm = this.emptyForm();

  constructor() {
    this.posts.refreshPages();
  }

  startAdd() {
    this.editingId = null;
    this.status.set('idle');
    this.errorMessage.set('');
    this.form = this.emptyForm();
  }

  editPage(page: Post) {
    this.editingId = page.id;
    this.status.set('idle');
    this.errorMessage.set('');
    this.form = {
      id: page.id,
      images: [...(page.images ?? [])],
      title_en: page.title_en || '',
      title_ar: page.title_ar || '',
      blocks_en: this.normalizeBlocks(page.blocks_en),
      blocks_ar: this.normalizeBlocks(page.blocks_ar)
    };
  }

  deletePage(page: Post) {
    if (!confirm(`Delete "${page.title_en || page.id}"? This cannot be undone.`)) return;

    this.status.set('saving');
    this.api.delete(`/api/pages/${encodeURIComponent(page.id)}`).subscribe({
      next: () => {
        this.status.set('done');
        if (this.editingId === page.id) { this.startAdd(); }
        this.posts.refreshPages();
      },
      error: (error: HttpErrorResponse) => this.fail(error)
    });
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
    this.errorMessage.set('');
    const payload = {
      id: this.form.id.trim(),
      images: this.form.images,
      title_en: this.form.title_en.trim(),
      title_ar: this.form.title_ar.trim(),
      blocks_en: this.form.blocks_en.map(block => ({ text: block.text.trim() })),
      blocks_ar: this.form.blocks_ar.map(block => ({ text: block.text.trim() }))
    };

    if (!payload.id || !payload.title_en || !payload.title_ar) {
      this.errorMessage.set('Fill ID and both titles.');
      this.status.set('error');
      return;
    }
    if (/\s/.test(payload.id)) {
      this.errorMessage.set('ID cannot contain spaces.');
      this.status.set('error');
      return;
    }

    this.status.set('saving');
    const request = this.editingId
      ? this.api.put(`/api/pages/${encodeURIComponent(this.editingId)}`, payload)
      : this.api.post('/api/pages', payload);

    request.subscribe({
      next: () => {
        this.startAdd();
        this.status.set('done');
        this.posts.refreshPages();
      },
      error: (error: HttpErrorResponse) => this.fail(error)
    });
  }

  uploadImages(event: Event) {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (!files.length) return;

    if (files.some(file => !/\.(jpg|jpeg|png|webp)$/i.test(file.name))) {
      this.errorMessage.set('Images must be .jpg, .jpeg, .png, or .webp.');
      this.status.set('error');
      return;
    }

    this.status.set('saving');
    this.errorMessage.set('');

    // Upload one after another so the images keep the order they were picked in.
    from(files).pipe(
      concatMap(file => {
        const data = new FormData();
        data.append('image', file);
        return this.api.post<{ img: string }>('/api/pages/upload', data);
      })
    ).subscribe({
      next: result => this.form.images.push(result.img),
      error: (error: HttpErrorResponse) => this.fail(error),
      complete: () => this.status.set('idle')
    });
  }

  removeImage(index: number) {
    this.form.images.splice(index, 1);
  }

  imageSrc(img: string) { return imageSrc(img); }

  private fail(error: HttpErrorResponse) {
    if (error.error?.error) this.errorMessage.set(error.error.error);
    else if (error.status === 0) this.errorMessage.set('Cannot connect to backend. Run npm run backend.');
    else this.errorMessage.set('Error saving. Check backend and required fields.');
    this.status.set('error');
  }

  private normalizeBlocks(blocks?: { text?: string }[] | null): Block[] {
    return blocks?.length ? blocks.map(block => ({ text: block.text || '' })) : [{ text: '' }];
  }

  private emptyForm(): PageForm {
    return { id: '', images: [], title_en: '', title_ar: '', blocks_en: [{ text: '' }], blocks_ar: [{ text: '' }] };
  }
}
