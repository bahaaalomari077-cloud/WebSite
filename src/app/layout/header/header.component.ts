import { Component, inject } from '@angular/core';
import { LanguageService } from '../../services/language.service';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  lang = inject(LanguageService);
  menuOpen = false;
}
