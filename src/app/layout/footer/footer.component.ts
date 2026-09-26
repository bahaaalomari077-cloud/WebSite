import { Component, inject } from '@angular/core';
import { LanguageService } from '../../services/language.service';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { PostsService } from '../../services/posts.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss'
})
export class FooterComponent {
  lang = inject(LanguageService);
  posts = inject(PostsService);

  constructor() {
    this.posts.refreshPages();
  }
}
