import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';
import { LanguageService } from './language.service';

export interface CmsLink { labelEn: string; labelAr: string; url: string; }
export interface SiteSettings {
  contactEmail: string;
  phones: string[];
  address: { en: string; ar: string };
  contactIntro: { en: string; ar: string };
  footerDescription: { en: string; ar: string };
  loginUrl: string;
  signupUrl: string;
  linkedInUrl: string;
  headerLinks: CmsLink[];
  footerLinks: CmsLink[];
  socialLinks: CmsLink[];
}

const defaults: SiteSettings = {
  contactEmail: 'support@credit-plus.me',
  phones: ['+962 79 600 8900', '+962 79 816 2208'],
  address: {
    en: 'King Hussein Business Park (KHBP), Core Building, First Floor, Amman, Jordan',
    ar: 'مجمع الملك الحسين للأعمال، مبنى Core، الطابق الأول، عمّان، الأردن'
  },
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
  headerLinks: [
    { labelEn: 'Home', labelAr: 'الرئيسية', url: '/home' },
    { labelEn: 'About Us', labelAr: 'من نحن', url: '/about' },
    { labelEn: 'Solutions for Suppliers', labelAr: 'حلول للموردين', url: '/suppliers' },
    { labelEn: 'Advantages for Buyers', labelAr: 'مزايا للمشترين', url: '/buyers' },
    { labelEn: 'Calculator', labelAr: 'الحاسبة', url: '/calculator' },
    { labelEn: 'Contact Us', labelAr: 'تواصل معنا', url: '/contact' }
  ],
  footerLinks: [
    { labelEn: 'Privacy Policy', labelAr: 'سياسة الخصوصية', url: '/privacy-policy' },
    { labelEn: 'News', labelAr: 'الأخبار', url: '/news' },
    { labelEn: 'Articles', labelAr: 'المقالات', url: '/articles' }
  ],
  socialLinks: [{ labelEn: 'LinkedIn', labelAr: 'لينكدإن', url: 'https://www.linkedin.com/company/credit-plus-me/' }]
};

@Injectable({ providedIn: 'root' })
export class CmsContentService {
  private readonly http = inject(HttpClient);
  readonly settings = signal<SiteSettings>(defaults);
  private readonly lang = inject(LanguageService);

  constructor() {
    this.http.get<Partial<SiteSettings>>('/cms-api/site-settings').pipe(catchError(() => of({} as Partial<SiteSettings>)))
      .subscribe(value => this.settings.set({ ...defaults, ...value,
        address: { ...defaults.address, ...value.address },
        contactIntro: { ...defaults.contactIntro, ...value.contactIntro },
        footerDescription: { ...defaults.footerDescription, ...value.footerDescription }
      }));
  }

  text(value: { en: string; ar: string } | undefined): string {
    const lang = this.lang.currentLang();
    return value?.[lang] || value?.en || '';
  }

  label(link: CmsLink): string {
    return link.labelAr && this.lang.currentLang() === 'ar' ? link.labelAr : link.labelEn;
  }

  isInternal(url: string): boolean { return url.startsWith('/'); }
}




