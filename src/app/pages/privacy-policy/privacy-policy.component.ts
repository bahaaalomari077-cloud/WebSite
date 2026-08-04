import { Component, computed, inject } from '@angular/core';
import { LanguageService } from '../../services/language.service';
import { SeoService } from '../../services/seo.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-privacy-policy',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './privacy-policy.component.html',
  styleUrl: './privacy-policy.component.scss'
})
export class PrivacyPolicyComponent {
  lang = inject(LanguageService);

  sections = computed(() => {
    this.lang.currentLang();
    return this.lang.get('privacy.sections') as { title: string; body: string }[];
  });

  constructor() {
    inject(SeoService).set({
      title:       'Privacy Policy',
      description: 'Read the Credit Plus Privacy Policy to learn what personal data we collect, why we collect it, who we share it with, and the choices available to you.',
      keywords:    'Credit Plus privacy policy, data protection, KYC, personal data'
    });
  }
}
