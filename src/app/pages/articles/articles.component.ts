import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../services/language.service';
import { PostsService, Post, imageSrc } from '../../services/posts.service';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-articles',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './articles.component.html',
  styleUrl: './articles.component.scss'
})
export class ArticlesComponent {
  lang  = inject(LanguageService);
  private postsService = inject(PostsService);

  posts = signal<Post[]>([]);

  constructor() {
    inject(SeoService).set({
      title:       'Articles & Insights',
      description: 'Read expert articles and insights on Supply Chain Finance, working capital, and fintech trends from the Credit Plus team.',
      keywords:    'SCF articles, supply chain finance insights, fintech articles, Credit Plus blog'
    });
    this.postsService.getArticles().subscribe({
      next:  (data) => this.posts.set(data),
      error: ()     => this.posts.set(this.lang.get('articles.posts'))
    });
  }

  getTitle(post: Post) { return this.lang.currentLang() === 'ar' ? post.title_ar : post.title_en; }
  getDate(post: Post)  { return this.lang.currentLang() === 'ar' ? post.date_ar  : post.date_en;  }
  imageSrc(post: Post) { return imageSrc(post.img); }
}
