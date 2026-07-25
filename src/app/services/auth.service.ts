import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);

  private readonly storageKey = 'cpAdminLoggedIn';
  isLoggedIn = signal<boolean>(this.hasToken());

  login(username: string, password: string) {
    return this.api.post<{ success: boolean }>('/api/admin/login', { username, password });
  }

  logout() {
    this.api.post('/api/admin/logout', {}).subscribe({
      next: () => this.clearSession(),
      error: () => this.clearSession()
    });
  }

  markLoggedIn() {
    localStorage.setItem(this.storageKey, 'true');
    this.isLoggedIn.set(true);
  }

  clearSession() {
    localStorage.removeItem(this.storageKey);
    this.isLoggedIn.set(false);
    this.router.navigate(['/login']);
  }

  private hasToken(): boolean {
    return typeof localStorage !== 'undefined' && localStorage.getItem(this.storageKey) === 'true';
  }
}
