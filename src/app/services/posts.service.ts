import { Injectable, inject, signal } from '@angular/core';
import { forkJoin, map } from 'rxjs';
import { ApiService, assetUrl } from './api.service';

export interface Post {
  id: string;
  img: string;
  date_en: string;
  date_ar: string;
  title_en: string;
  title_ar: string;
  blocks_en?: PostBlock[] | null;
  blocks_ar?: PostBlock[] | null;
  sort_order?: number;
}

export interface PostBlock {
  text?: string;
  type?: string;
  items?: string[];
}

export function imageSrc(img: string): string {
  return assetUrl(img);
}

@Injectable({ providedIn: 'root' })
export class PostsService {
  private api = inject(ApiService);

  // Footer pages are shared between the footer and the page editor.
  pages = signal<Post[]>([]);

  refreshPages() {
    this.getPages().subscribe({
      next: pages => this.pages.set(pages),
      error: () => this.pages.set([])
    });
  }

  getNews()     { return this.api.get<Post[]>('/api/news'); }
  getArticles() { return this.api.get<Post[]>('/api/articles'); }
  getPages()    { return this.api.get<Post[]>('/api/pages'); }

  getPage(id: string) {
    return this.getPages().pipe(map(pages => pages.find(page => page.id === id) ?? null));
  }

  getPost(id: string) {
    return forkJoin([this.getNews(), this.getArticles()]).pipe(
      map(([news, articles]) => [...news, ...articles].find(post => post.id === id) ?? null)
    );
  }
}
