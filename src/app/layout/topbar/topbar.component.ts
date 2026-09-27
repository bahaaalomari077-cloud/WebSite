import { Component, inject } from '@angular/core';
import { LanguageService } from '../../services/language.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { CmsContentService } from '../../services/cms-content.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss'
})
export class TopbarComponent {
  lang = inject(LanguageService);
  cms = inject(CmsContentService);
}
