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
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  tab: 'news' | 'articles' = 'news';
  selectedType: 'news' | 'articles' | null = null;
  editingId: string | null = null;
  status = signal<'idle' | 'loading' | 'saving' | 'done' | 'error'>('idle');
  posts = signal<AdminPost[]>([]);

  form: PostForm = this.emptyForm();

  showTabs = true;

  constructor() {
    this.route.queryParams.subscribe(p => {
      if (p['type'] === 'articles') { this.selectType('articles'); this.showTabs = false; }
      else if (p['type'] === 'news') { this.selectType('news'); this.showTabs = false; }
      else { this.showTabs = true; }
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
    this.form = this.emptyForm();
  }

  editPost(post: AdminPost) {
    this.editingId = post.id;
    this.status.set('idle');
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

  loadPosts() {
    if (!this.selectedType) return;

    this.status.set('loading');
    this.http.get<AdminPost[]>(`/api/${this.tab}`).subscribe({
      next: data => {
        this.posts.set(data);
        this.status.set('idle');
      },
      error: () => {
        this.posts.set([]);
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
    this.status.set('saving');
    const endpoint = `/api/${this.tab}`;
    const request = this.editingId
      ? this.http.put(`${endpoint}/${encodeURIComponent(this.editingId)}`, this.form)
      : this.http.post(endpoint, this.form);

    request.subscribe({
      next: () => {
        this.status.set('done');
        this.startAdd();
        this.loadPosts();
      },
      error: () => this.status.set('error')
    });
  }

  private normalizeBlocks(blocks?: Block[] | null): Block[] {
    return blocks?.length ? blocks.map(block => ({ text: block.text || '' })) : [{ text: '' }];
  }
}
