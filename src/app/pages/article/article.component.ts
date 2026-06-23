import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LanguageService } from '../../services/language.service';
import { SeoService } from '../../services/seo.service';

type ArticleBlock =
  | { type: 'p'; text: string }
  | { type: 'h'; text: string }
  | { type: 'ul'; items: string[] };

interface ArticleData {
  crumb: string;
  title: string;
  meta: string;
  image: string;
  blocks: ArticleBlock[];
}

@Component({
  selector: 'app-article',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './article.component.html',
  styleUrl: './article.component.scss'
})
export class ArticleComponent {
  lang = inject(LanguageService);
  currentLang = this.lang.currentLang;
  private route = inject(ActivatedRoute);

  articleId = this.route.snapshot.paramMap.get('id') || 'hb';
  get article(): ArticleData { return this.lang.get('article.' + this.articleId); }

  constructor() {
    const seo     = inject(SeoService);
    const article = this.lang.get('article.' + this.articleId);
    if (article) {
      seo.setArticle({
        title:       article.title,
        description: article.meta || article.title,
        image:       article.image,
        type:        'article',
        keywords:    'supply chain finance, Credit Plus, ' + article.title
      });
    }
  }
}