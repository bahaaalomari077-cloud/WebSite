import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LanguageService } from '../../services/language.service';
import { Post, PostsService, imageSrc } from '../../services/posts.service';
import { SeoService } from '../../services/seo.service';
import { CmsContentService } from '../../services/cms-content.service';

@Component({
  selector: 'app-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './page.component.html',
  styleUrl: '../article/article.component.scss'
})
export class PageComponent {
  lang = inject(LanguageService);
  cms = inject(CmsContentService);
  private route = inject(ActivatedRoute);
  private postsService = inject(PostsService);
  private seo = inject(SeoService);

  page = signal<Post | null>(null);
  status = signal<'loading' | 'ready' | 'missing'>('loading');

  view = computed(() => {
    const page = this.page();
    if (!page) return null;

    const isArabic = this.lang.currentLang() === 'ar';
    const blocks = (isArabic ? page.blocks_ar : page.blocks_en) ?? [];
    return {
      title: isArabic ? page.title_ar : page.title_en,
      images: (page.images ?? []).map(img => imageSrc(img)),
      paragraphs: blocks.map(block => block.text?.trim() ?? '').filter(text => text)
    };
  });

  constructor() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id') ?? '';
      this.status.set('loading');
      this.postsService.getPage(id).subscribe({
        next: page => {
          this.page.set(page);
          this.status.set(page ? 'ready' : 'missing');
          const view = this.view();
          if (view) {
            this.seo.setArticle({
              title: view.title,
              description: view.paragraphs[0] || view.title,
              image: view.images[0] ?? '',
              keywords: 'Credit Plus, ' + view.title
            });
          }
        },
        error: () => this.status.set('missing')
      });
    });
  }
}

