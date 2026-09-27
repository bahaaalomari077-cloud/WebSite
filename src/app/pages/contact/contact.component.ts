import { AfterViewInit, Component, inject } from '@angular/core';
import { LanguageService } from '../../services/language.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { SeoService } from '../../services/seo.service';
import { CmsContentService } from '../../services/cms-content.service';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss'
})
export class ContactComponent implements AfterViewInit {
  lang = inject(LanguageService);
  cms = inject(CmsContentService);

  constructor() {
    inject(SeoService).set({
      title: 'Contact Us',
      description: "Get in touch with the Credit Plus team. We're here to answer questions about our Supply Chain Finance platform.",
      keywords: 'contact Credit Plus, support, SCF Jordan, get in touch'
    });
  }

  ngAfterViewInit(): void {
    const script = document.createElement('script');
    script.src = 'https://js-eu1.hsforms.net/forms/embed/149387713.js';
    script.defer = true;
    document.body.appendChild(script);
  }
}
