import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { AboutComponent } from './pages/about/about.component';
import { SuppliersComponent } from './pages/suppliers/suppliers.component';
import { BuyersComponent } from './pages/buyers/buyers.component';
import { CalculatorComponent } from './pages/calculator/calculator.component';
import { ContactComponent } from './pages/contact/contact.component';
import { ArticleComponent } from './pages/article/article.component';
import { NewsComponent } from './pages/news/news.component';
import { ArticlesComponent } from './pages/articles/articles.component';
import { AdminComponent } from './pages/admin/admin.component';
import { LoginComponent } from './pages/login/login.component';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'about', component: AboutComponent },
  { path: 'suppliers', component: SuppliersComponent },
  { path: 'buyers', component: BuyersComponent },
  { path: 'calculator', component: CalculatorComponent },
  { path: 'contact', component: ContactComponent },
  { path: 'news', component: NewsComponent },
  { path: 'articles', component: ArticlesComponent },
  { path: 'article/:id', component: ArticleComponent },
  { path: 'login', component: LoginComponent },
  { path: 'admin', component: AdminComponent, canActivate: [AuthGuard] },
  { path: '**', redirectTo: 'home' }
];
