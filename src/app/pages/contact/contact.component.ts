import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LanguageService } from '../../services/language.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { EmailService } from '../../services/email.service';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [TranslatePipe, FormsModule],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss'
})
export class ContactComponent {
  lang = inject(LanguageService);
  private email = inject(EmailService);

  constructor() {
    inject(SeoService).set({
      title:       'Contact Us',
      description: 'Get in touch with the Credit Plus team. We\'re here to answer questions about our Supply Chain Finance platform.',
      keywords:    'contact Credit Plus, support, SCF Jordan, get in touch'
    });
  }

  form = { name: '', email: '', company: '', message: '' };
  status: 'idle' | 'sending' | 'sent' | 'error' = 'idle';

  submit() {
    if (!this.form.name || !this.form.email || !this.form.message) return;
    this.status = 'sending';
    this.email.send(this.form).subscribe({
      next: () => {
        this.status = 'sent';
        this.form = { name: '', email: '', company: '', message: '' };
      },
      error: (err) => { console.error('EmailJS error:', err); this.status = 'error'; }
    });
  }
}
