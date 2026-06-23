import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../services/language.service';
import { PostsService, Post } from '../../services/posts.service';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-news',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './news.component.html',
  styleUrl: './news.component.scss'
})
export class NewsComponent {
  lang  = inject(LanguageService);
  private postsService = inject(PostsService);

  posts = signal<Post[]>([]);

  constructor() {
    inject(SeoService).set({
      title:       'Latest News',
      description: 'Stay up to date with the latest news from Credit Plus — Supply Chain Finance announcements, partnerships, and milestones in Jordan.',
      keywords:    'Credit Plus news, SCF news, supply chain finance Jordan, fintech news'
    });
    this.postsService.getNews().subscribe({
      next:  (data) => this.posts.set(data),
      error: ()     => this.posts.set(this.lang.get('news.posts'))
    });
  }

  getTitle(post: Post) { return this.lang.currentLang() === 'ar' ? post.title_ar : post.title_en; }
  getDate(post: Post)  { return this.lang.currentLang() === 'ar' ? post.date_ar  : post.date_en;  }
}