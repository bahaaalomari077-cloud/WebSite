import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../services/language.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { PostsService, Post } from '../../services/posts.service';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  private langSvc = inject(LanguageService);
  private postsSvc = inject(PostsService);
  private seo       = inject(SeoService);
  currentLang = this.langSvc.currentLang;
  faqOpen: number | null = null;

  faqs = this.langSvc.get('home.faqs');
  news     = signal<Post[]>([]);
  articles = signal<Post[]>([]);

  constructor() {
    this.seo.set({
      title:       'Supply Chain Finance Platform',
      description: 'Credit Plus connects buyers, suppliers, and financial institutions through a seamless Supply Chain Finance platform in Jordan and the Middle East.',
      keywords:    'supply chain finance, Jordan, fintech, reverse factoring, SCF, Credit Plus'
    });
    this.postsSvc.getNews().subscribe({
      next:  (d) => this.news.set(d.slice(0, 3)),
      error: ()  => this.news.set(this.langSvc.get('news.posts').slice(0, 3))
    });
    this.postsSvc.getArticles().subscribe({
      next:  (d) => this.articles.set(d.slice(0, 3)),
      error: ()  => this.articles.set(this.langSvc.get('articles.posts').slice(0, 3))
    });
  }

  toggleFaq(i: number) { this.faqOpen = this.faqOpen === i ? null : i; }

  getTitle(post: Post) { return this.currentLang() === 'ar' ? post.title_ar : post.title_en; }
  getDate(post: Post)  { return this.currentLang() === 'ar' ? post.date_ar  : post.date_en;  }
}
