import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { Router } from '@angular/router';

export interface SeoData {
  title:       string;
  description: string;
  image?:      string;
  type?:       'website' | 'article';
  keywords?:   string;
}

const SITE_NAME = 'Credit Plus';
const SITE_URL  = 'https://www.credit-plus.me';
const DEFAULT_IMAGE = `${SITE_URL}/og-image.jpg`;

@Injectable({ providedIn: 'root' })
export class SeoService {
  private meta   = inject(Meta);
  private title  = inject(Title);
  private router = inject(Router);

  set(data: SeoData) {
    const fullTitle = `${data.title} | ${SITE_NAME}`;
    const url       = `${SITE_URL}${this.router.url}`;
    const image     = data.image || DEFAULT_IMAGE;
    const type      = data.type  || 'website';

    this.title.setTitle(fullTitle);

    const tags: { name?: string; property?: string; content: string }[] = [
      { name: 'description',           content: data.description },
      { name: 'keywords',              content: data.keywords || 'supply chain finance, Jordan, fintech, Credit Plus' },
      { name: 'robots',                content: 'index, follow' },
      { property: 'og:title',          content: fullTitle },
      { property: 'og:description',    content: data.description },
      { property: 'og:url',            content: url },
      { property: 'og:image',          content: image },
      { property: 'og:type',           content: type },
      { property: 'og:site_name',      content: SITE_NAME },
      { property: 'og:locale',         content: 'en_US' },
      { name: 'twitter:card',          content: 'summary_large_image' },
      { name: 'twitter:title',         content: fullTitle },
      { name: 'twitter:description',   content: data.description },
      { name: 'twitter:image',         content: image },
    ];

    tags.forEach(tag => {
      if (tag.property) this.meta.updateTag({ property: tag.property, content: tag.content });
      else              this.meta.updateTag({ name: tag.name!, content: tag.content });
    });

    this.setCanonical(url);
  }

  setArticle(data: SeoData & { datePublished?: string; author?: string }) {
    this.set({ ...data, type: 'article' });
    if (data.datePublished) {
      this.meta.updateTag({ property: 'article:published_time', content: data.datePublished });
    }
  }

  private setCanonical(url: string) {
    let link: HTMLLinkElement | null = document.querySelector("link[rel='canonical']");
    if (!link) {
      link = document.createElement('link');
      link.setAttribute('rel', 'canonical');
      document.head.appendChild(link);
    }
    link.setAttribute('href', url);
  }
}
