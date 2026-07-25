import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, map } from 'rxjs';

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
  return img?.startsWith('http') || img?.startsWith('/') ? img : `/${img}`;
}

@Injectable({ providedIn: 'root' })
export class PostsService {
  private http = inject(HttpClient);

  getNews()     { return this.http.get<Post[]>('/api/news'); }
  getArticles() { return this.http.get<Post[]>('/api/articles'); }

  getPost(id: string) {
    return forkJoin([this.getNews(), this.getArticles()]).pipe(
      map(([news, articles]) => [...news, ...articles].find(post => post.id === id) ?? null)
    );
  }
}
