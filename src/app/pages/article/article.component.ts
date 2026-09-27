import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LanguageService } from '../../services/language.service';
import { Post, PostBlock, PostsService, imageSrc } from '../../services/posts.service';
import { SeoService } from '../../services/seo.service';
import { CmsContentService } from '../../services/cms-content.service';

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
  cms = inject(CmsContentService);
  currentLang = this.lang.currentLang;
  private route = inject(ActivatedRoute);
  private postsService = inject(PostsService);

  articleId = this.route.snapshot.paramMap.get('id') || 'hb';
  post = signal<Post | null>(null);

  article = computed<ArticleData>(() => {
    const post = this.post();
    if (post) {
      const isArabic = this.lang.currentLang() === 'ar';
      return {
        crumb: 'News & Articles',
        title: isArabic ? post.title_ar : post.title_en,
        meta: isArabic ? post.date_ar : post.date_en,
        image: imageSrc(post.img),
        blocks: this.blocksFor(post, isArabic)
      };
    }

    return this.lang.get('article.' + this.articleId);
  });

  constructor() {
    const seo = inject(SeoService);
    this.postsService.getPost(this.articleId).subscribe({
      next: post => {
        this.post.set(post);
        this.setSeo(seo, this.article());
      },
      error: () => this.setSeo(seo, this.article())
    });
  }

  private blocksFor(post: Post, isArabic: boolean): ArticleBlock[] {
    const blocks = isArabic ? post.blocks_ar : post.blocks_en;
    const normalized = this.normalizeBlocks(blocks);
    if (normalized.length) return normalized;

    const translated = this.lang.get('article.' + post.id);
    if (translated?.blocks?.length) return translated.blocks;

    return [{ type: 'p', text: isArabic ? post.title_ar : post.title_en }];
  }

  private normalizeBlocks(blocks?: PostBlock[] | null): ArticleBlock[] {
    if (!blocks?.length) return [];

    return blocks
      .map(block => {
        if (block.type === 'h' && block.text) return { type: 'h', text: block.text } as ArticleBlock;
        if (block.type === 'ul' && block.items?.length) return { type: 'ul', items: block.items } as ArticleBlock;
        if (block.text) return { type: 'p', text: block.text } as ArticleBlock;
        return null;
      })
      .filter((block): block is ArticleBlock => block !== null);
  }

  private setSeo(seo: SeoService, article: ArticleData) {
    seo.setArticle({
      title: article.title,
      description: article.meta || article.title,
      image: article.image,
      type: 'article',
      keywords: 'supply chain finance, Credit Plus, ' + article.title
    });
  }
}

