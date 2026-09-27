import { Component } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { TopbarComponent } from './layout/topbar/topbar.component';
import { HeaderComponent } from './layout/header/header.component';
import { FooterComponent } from './layout/footer/footer.component';
import { PageElementsComponent } from './layout/page-elements/page-elements.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, TopbarComponent, HeaderComponent, FooterComponent, PageElementsComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'credit-plus-angular';

  constructor(private router: Router) {}

  get isAdminRoute(): boolean {
    const url = this.router.url;
    return url.startsWith('/admin') || url.startsWith('/login');
  }
}

