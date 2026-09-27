import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from '../services/language.service';
import { CmsContentService } from '../services/cms-content.service';

@Pipe({ name: 'translate', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  private lang = inject(LanguageService);
  private cms = inject(CmsContentService);

  transform(key: string): string {
    return this.lang.t(key)
      .replaceAll('mailto:support@credit-plus.me', `mailto:${this.cms.settings().contactEmail}`)
      .replaceAll('support@credit-plus.me', this.cms.settings().contactEmail);
  }
}
