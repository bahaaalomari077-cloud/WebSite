import { Component, inject } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss'
})
export class AboutComponent {
  constructor() {
    inject(SeoService).set({
      title:       'About Us',
      description: 'Learn about Credit Plus — the Supply Chain Finance platform built for Jordan and the Middle East, backed by JOPACC and trusted by leading banks.',
      keywords:    'about Credit Plus, SCF Jordan, fintech company, JOPACC, supply chain'
    });
  }
}
