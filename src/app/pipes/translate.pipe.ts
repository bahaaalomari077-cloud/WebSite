import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from '../services/language.service';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false // impure so it reacts to language signal changes
})
export class TranslatePipe implements PipeTransform {
  private lang = inject(LanguageService);

  transform(key: string): string {
    return this.lang.t(key);
  }
}
