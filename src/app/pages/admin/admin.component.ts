import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { imageSrc } from '../../services/posts.service';
import { AuthService } from '../../services/auth.service';
import { ApiService } from '../../services/api.service';

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

interface AdminPost extends Omit<PostForm, 'blocks_en' | 'blocks_ar'> {
  blocks_en?: Block[] | null;
  blocks_ar?: Block[] | null;
  sort_order?: number;
}

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);

  tab: 'news' | 'articles' = 'news';
  selectedType: 'news' | 'articles' | null = null;
  editingId: string | null = null;
  status = signal<'idle' | 'loading' | 'saving' | 'done' | 'error'>('idle');
  errorMessage = signal('');
  posts = signal<AdminPost[]>([]);

  form: PostForm = this.emptyForm();

  showTabs = true;

  constructor() {
    this.route.queryParams.subscribe(p => {
      if (p['type'] === 'articles') { this.selectType('articles'); this.showTabs = false; }
      else if (p['type'] === 'news') { this.selectType('news'); this.showTabs = false; }
      else {
        this.showTabs = true;
        this.selectType(this.tab);
      }
    });
  }

  selectType(type: 'news' | 'articles') {
    this.tab = type;
    this.selectedType = type;
    this.startAdd();
    this.loadPosts();
  }

  startAdd() {
    this.editingId = null;
    this.status.set('idle');
    this.errorMessage.set('');
    this.form = this.emptyForm();
  }

  editPost(post: AdminPost) {
    this.editingId = post.id;
    this.status.set('idle');
    this.errorMessage.set('');
    this.form = {
      id: post.id,
      img: post.img || '',
      date_en: post.date_en || '',
      date_ar: post.date_ar || '',
      title_en: post.title_en || '',
      title_ar: post.title_ar || '',
      blocks_en: this.normalizeBlocks(post.blocks_en),
      blocks_ar: this.normalizeBlocks(post.blocks_ar)
    };
  }

  deletePost(post: AdminPost) {
    if (!confirm(`Delete "${post.title_en || post.id}"? This cannot be undone.`)) return;

    this.errorMessage.set('');
    this.status.set('saving');
    this.api.delete(`/api/${this.tab}/${encodeURIComponent(post.id)}`).subscribe({
      next: () => {
        this.status.set('done');
        if (this.editingId === post.id) { this.startAdd(); }
        this.loadPosts();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.readError(error));
        this.status.set('error');
      }
    });
  }

  loadPosts() {
    if (!this.selectedType) return;

    this.status.set('loading');
    this.api.get<AdminPost[]>(`/api/${this.tab}`).subscribe({
      next: data => {
        this.posts.set(data);
        this.status.set('idle');
      },
      error: () => {
        this.posts.set([]);
        this.errorMessage.set('Could not load items. Make sure the backend is running.');
        this.status.set('error');
      }
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
    this.errorMessage.set('');
    const payload = this.preparePayload();
    const validationError = this.validatePayload(payload);

    if (validationError) {
      this.errorMessage.set(validationError);
      this.status.set('error');
      return;
    }

    this.status.set('saving');
    const endpoint = `/api/${this.tab}`;
    const request = this.editingId
      ? this.api.put(`${endpoint}/${encodeURIComponent(this.editingId)}`, payload)
      : this.api.post(endpoint, payload);

    request.subscribe({
      next: () => {
        this.status.set('done');
        this.startAdd();
        this.loadPosts();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.readError(error));
        this.status.set('error');
      }
    });
  }

  uploadImage(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!/\.(jpg|jpeg|png|webp|svg)$/i.test(file.name)) {
      this.errorMessage.set('Image must be .jpg, .jpeg, .png, .webp, or .svg.');
      this.status.set('error');
      input.value = '';
      return;
    }

    const data = new FormData();
    data.append('image', file);
    this.status.set('saving');
    this.errorMessage.set('');

    this.api.post<{ img: string }>('/api/upload', data).subscribe({
      next: result => {
        this.form.img = result.img;
        this.status.set('idle');
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage.set(this.readError(error));
        this.status.set('error');
      }
    });
  }

  private normalizeBlocks(blocks?: Block[] | null): Block[] {
    return blocks?.length ? blocks.map(block => ({ text: block.text || '' })) : [{ text: '' }];
  }

  private preparePayload(): PostForm {
    return {
      id: this.form.id.trim(),
      img: this.form.img.trim(),
      date_en: this.form.date_en.trim(),
      date_ar: this.form.date_ar.trim(),
      title_en: this.form.title_en.trim(),
      title_ar: this.form.title_ar.trim(),
      blocks_en: this.form.blocks_en.map(block => ({ text: block.text.trim() })),
      blocks_ar: this.form.blocks_ar.map(block => ({ text: block.text.trim() }))
    };
  }

  private readError(error: HttpErrorResponse): string {
    if (error.status === 401) {
      this.auth.clearSession();
      return 'Session expired. Please log in again.';
    }
    if (typeof error.error === 'string' && error.error.trim()) return error.error;
    if (error.error?.error) return error.error.error;
    if (error.error?.detail) return error.error.detail;
    if (error.status === 0) return 'Cannot connect to backend. Run npm run backend.';
    return 'Error saving. Check backend and required fields.';
  }

  logout() {
    this.auth.logout();
  }

  imageSrc(img: string) { return imageSrc(img); }

  private validatePayload(payload: PostForm): string | null {
    if (!payload.id || !payload.img || !payload.date_en || !payload.date_ar || !payload.title_en || !payload.title_ar) {
      return 'Fill all required fields.';
    }

    if (/\s/.test(payload.id)) {
      return 'ID cannot contain spaces.';
    }

    if (!/\.(jpg|jpeg|png|webp|svg)$/i.test(payload.img)) {
      return 'Image filename must end with .jpg, .jpeg, .png, .webp, or .svg.';
    }

    return null;
  }
}
