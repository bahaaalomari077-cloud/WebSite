import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface Post {
  id: string;
  img: string;
  date_en: string;
  date_ar: string;
  title_en: string;
  title_ar: string;
}

@Injectable({ providedIn: 'root' })
export class PostsService {
  private http = inject(HttpClient);

  getNews()     { return this.http.get<Post[]>('/api/news'); }
  getArticles() { return this.http.get<Post[]>('/api/articles'); }
}
