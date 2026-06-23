import { Injectable, signal } from '@angular/core';
import { en } from '../i18n/en';
import { ar } from '../i18n/ar';

type Dict = typeof en;

@Injectable({ providedIn: 'root' })
export class LanguageService {
  currentLang = signal<'en' | 'ar'>('en');

  private dicts: Record<string, Dict> = { en, ar };

  constructor() {
    this.applyDir('en');
  }

  setLang(lang: 'en' | 'ar') {
    this.currentLang.set(lang);
    this.applyDir(lang);
    // Apply to legacy data-en/data-ar elements in page components
    setTimeout(() => this.applyDataAttrs(lang), 20);
    this.syncCalcLang();
  }

  /** Look up a dot-notation key from the active language dict */
  t(key: string): string {
    const dict = this.dicts[this.currentLang()] as any;
    const result = key.split('.').reduce((obj, k) => obj?.[k], dict);
    return typeof result === 'string' ? result : key;
  }

  /** Look up any value (string, array, object) from the active language dict */
  get(key: string): any {
    const dict = this.dicts[this.currentLang()] as any;
    return key.split('.').reduce((obj, k) => obj?.[k], dict);
  }

  private applyDir(lang: 'en' | 'ar') {
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }

  private applyDataAttrs(lang: 'en' | 'ar') {
    document.querySelectorAll<HTMLElement>('[data-en]').forEach(el => {
      const text = lang === 'ar'
        ? el.getAttribute('data-ar')
        : el.getAttribute('data-en');
      if (text !== null) el.innerHTML = text;
    });
  }

  private syncCalcLang() {
    const f = document.getElementById('calcFrame') as HTMLIFrameElement;
    if (f?.contentWindow) {
      try { f.contentWindow.postMessage({ cpSetLang: this.currentLang() }, '*'); } catch (_) { }
    }
  }
}
