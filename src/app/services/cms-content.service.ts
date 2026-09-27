import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LanguageService } from './language.service';

export interface CmsLink {
  labelEn: string;
  labelAr: string;
  url: string;
  section?: string;
}

export interface CmsPageLink {
  pagePath: string;
  linkKey: string;
  labelEn: string;
  labelAr: string;
  url: string;
  sortOrder: number;
}

export interface CmsPagePlacement {
  id: string;
  pagePath: string;
  elementType: 'Phone' | 'Link' | string;
  labelEn: string;
  labelAr: string;
  value: string;
  placement: 'Top' | 'Bottom' | string;
  sortOrder: number;
}

export interface SiteSettings {
  contactEmail: string;
  phones: string[];
  address: { en: string; ar: string };
  addressMapUrl: string;
  contactIntro: { en: string; ar: string };
  footerDescription: { en: string; ar: string };
  loginUrl: string;
  signupUrl: string;
  linkedInUrl: string;
  headerLogo: string;
  footerLogo: string;
  headerLogoUrl: string;
  footerLogoUrl: string;
  headerLinks: CmsLink[];
  footerLinks: CmsLink[];
  socialLinks: CmsLink[];
  pageLinks: CmsPageLink[];
  pagePlacements: CmsPagePlacement[];
}

const defaults: SiteSettings = {
  contactEmail: 'support@credit-plus.me',
  phones: ['+962 79 600 8900', '+962 79 816 2208'],
  address: {
    en: 'King Hussein Business Park (KHBP), Core Building, First Floor, Amman, Jordan',
    ar: 'مجمع الملك الحسين للأعمال، مبنى Core، الطابق الأول، عمّان، الأردن'
  },
  addressMapUrl: 'https://www.google.com/maps/search/?api=1&query=King+Hussein+Business+Park+Amman+Jordan',
  contactIntro: {
    en: "We'd love to hear from you. For inquiries, partnerships, or support, feel free to reach out using the details below.",
    ar: 'يسعدنا التواصل معكم. للاستفسارات أو الشراكات أو الدعم، تواصلوا معنا عبر التفاصيل أدناه.'
  },
  footerDescription: {
    en: 'Credit Plus helps businesses manage and grow their cash flow.',
    ar: 'كريديت بلس تساعد الشركات على إدارة وتنمية تدفقاتها النقدية.'
  },
  loginUrl: 'https://cp.credit-plus.me/Account/Login',
  signupUrl: 'https://cp.credit-plus.me/Account/OnBoarding',
  linkedInUrl: 'https://www.linkedin.com/company/credit-plus-me/',
  headerLogo: '/cms-media/credit-plus/header-logo.png',
  footerLogo: '/cms-media/credit-plus/footer-logo.png',
  headerLogoUrl: '/home',
  footerLogoUrl: '/home',
  headerLinks: [
    { labelEn: 'Home', labelAr: 'الرئيسية', url: '/home' },
    { labelEn: 'About Us', labelAr: 'من نحن', url: '/about' },
    { labelEn: 'Solutions for Suppliers', labelAr: 'حلول للموردين', url: '/suppliers' },
    { labelEn: 'Advantages for Buyers', labelAr: 'مزايا للمشترين', url: '/buyers' },
    { labelEn: 'Calculator', labelAr: 'الحاسبة', url: '/calculator' },
    { labelEn: 'Contact Us', labelAr: 'تواصل معنا', url: '/contact' }
  ],
  footerLinks: [
    { labelEn: 'Privacy Policy', labelAr: 'سياسة الخصوصية', url: '/privacy-policy', section: 'Explore' },
    { labelEn: 'News', labelAr: 'الأخبار', url: '/news', section: 'Company' },
    { labelEn: 'Articles', labelAr: 'المقالات', url: '/articles', section: 'Company' }
  ],
  socialLinks: [{ labelEn: 'LinkedIn', labelAr: 'لينكدإن', url: 'https://www.linkedin.com/company/credit-plus-me/' }],
  pageLinks: [],
  pagePlacements: []
};

@Injectable({ providedIn: 'root' })
export class CmsContentService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  readonly settings = signal<SiteSettings>(defaults);
  private readonly lang = inject(LanguageService);

  constructor() {
    this.http.get<Partial<SiteSettings>>('/cms-api/site-settings')
      .pipe(catchError(() => of({} as Partial<SiteSettings>)))
      .subscribe(value => this.settings.set({
        ...defaults,
        ...value,
        headerLogo: value.headerLogo || defaults.headerLogo,
        footerLogo: value.footerLogo || defaults.footerLogo,
        headerLogoUrl: value.headerLogoUrl || defaults.headerLogoUrl,
        footerLogoUrl: value.footerLogoUrl || defaults.footerLogoUrl,
        addressMapUrl: value.addressMapUrl || defaults.addressMapUrl,
        headerLinks: value.headerLinks ?? defaults.headerLinks,
        footerLinks: value.footerLinks ?? defaults.footerLinks,
        socialLinks: value.socialLinks ?? defaults.socialLinks,
        pageLinks: value.pageLinks ?? defaults.pageLinks,
        pagePlacements: value.pagePlacements ?? defaults.pagePlacements,
        address: { ...defaults.address, ...value.address },
        contactIntro: { ...defaults.contactIntro, ...value.contactIntro },
        footerDescription: { ...defaults.footerDescription, ...value.footerDescription }
      }));
  }

  text(value: { en: string; ar: string } | undefined): string {
    const lang = this.lang.currentLang();
    return value?.[lang] || value?.en || '';
  }

  label(link: CmsLink | CmsPagePlacement | CmsPageLink): string {
    return link.labelAr && this.lang.currentLang() === 'ar' ? link.labelAr : link.labelEn;
  }

  isInternal(url: string): boolean {
    return url.startsWith('/');
  }

  routerTarget(url: string): UrlTree | null {
    return this.isInternal(url) ? this.router.parseUrl(url) : null;
  }

  externalHref(url: string): string | null {
    return url && !this.isInternal(url) ? url : null;
  }

  pageUrl(pagePath: string, linkKey: string, fallback: string): string {
    const normalizedPath = this.normalizePath(pagePath);
    const match = this.settings().pageLinks.find(link =>
      link.linkKey.toLowerCase() === linkKey.toLowerCase() &&
      this.matchesPath(link.pagePath, normalizedPath));
    return match?.url || fallback;
  }

  pageRouterLink(pagePath: string, linkKey: string, fallback: string): UrlTree | null {
    return this.routerTarget(this.pageUrl(pagePath, linkKey, fallback));
  }

  pageHref(pagePath: string, linkKey: string, fallback: string): string | null {
    return this.externalHref(this.pageUrl(pagePath, linkKey, fallback));
  }

  footerLinksFor(section: string): CmsLink[] {
    return this.settings().footerLinks.filter(link =>
      (link.section || 'Explore').toLowerCase() === section.toLowerCase());
  }

  pagePlacementsFor(path: string, placement: 'top' | 'bottom'): CmsPagePlacement[] {
    const normalizedPath = this.normalizePath(path);
    return this.settings().pagePlacements
      .filter(item =>
        this.matchesPath(item.pagePath, normalizedPath) &&
        ((item.placement || 'Bottom').toLowerCase() === 'top' ? placement === 'top' : placement === 'bottom')
      )
      .sort((a, b) => a.sortOrder - b.sortOrder);
  }

  private matchesPath(pattern: string, path: string): boolean {
    const patternParts = this.normalizePath(pattern).split('/').filter(Boolean);
    const pathParts = path.split('/').filter(Boolean);
    return patternParts.length === pathParts.length &&
      patternParts.every((part, index) => part.startsWith(':') || part === '*' ||
        part.toLowerCase() === pathParts[index].toLowerCase());
  }

  private normalizePath(path: string): string {
    const pathname = (path || '/').split(/[?#]/, 1)[0];
    const trimmed = pathname.replace(/\/+$/, '');
    return trimmed || '/';
  }
}
