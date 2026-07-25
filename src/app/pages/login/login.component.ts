import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  username = '';
  password = '';
  error = signal('');
  loading = signal(false);

  login() {
    if (!this.username || !this.password) {
      this.error.set('Enter username and password.');
      return;
    }

    this.error.set('');
    this.loading.set(true);

    this.auth.login(this.username, this.password).subscribe({
      next: (result) => {
        this.loading.set(false);
        if (result.success) {
          this.auth.markLoggedIn();
          this.router.navigate(['/admin']);
        } else {
          this.error.set('Login failed.');
        }
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(error.status === 401 ? 'Invalid username or password.' : 'Could not connect to backend.');
      }
    });
  }
}
