import { bootstrapApplication } from '@angular/platform-browser';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

type Session = { email: string; name: string; roles: string[] };

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule],
  template: `
    <main>
      <h1>Equipment Tracking</h1>
      @if (session; as current) {
        <p>Signed in as <strong>{{ current.name }}</strong> ({{ current.roles.join(', ') }})</p>
        <button type="button" (click)="logout()">Sign out</button>
        <p class="success">Your local review session is active.</p>
      } @else {
        <form (ngSubmit)="login()" #loginForm="ngForm">
          <label for="email">Email</label>
          <input id="email" name="email" type="email" [(ngModel)]="email" required autocomplete="username">
          <label for="password">Password</label>
          <input id="password" name="password" type="password" [(ngModel)]="password" required autocomplete="current-password">
          <button type="submit" [disabled]="loginForm.invalid || loading">{{ loading ? 'Signing in…' : 'Sign in' }}</button>
        </form>
        @if (error) { <p class="error" role="alert">{{ error }}</p> }
      }
    </main>
  `,
  styles: [`main{font-family:system-ui;max-width:32rem;margin:4rem auto;padding:1rem}h1{color:#164e63}form{display:grid;gap:.5rem}input{font:inherit;padding:.5rem;margin-bottom:.75rem}button{font:inherit;padding:.55rem .8rem;width:max-content}.error{color:#b42318}.success{color:#067647}`]
})
class AppComponent implements OnInit {
  email = '';
  password = '';
  session: Session | null = null;
  error = '';
  loading = false;

  async ngOnInit(): Promise<void> {
    try {
      const response = await fetch('/api/auth/me', { credentials: 'same-origin' });
      if (response.ok) this.session = await response.json() as Session;
    } catch {
      this.error = 'The local sign-in service is unavailable.';
    }
  }

  async login(): Promise<void> {
    this.error = '';
    this.loading = true;
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'same-origin',
        body: JSON.stringify({ email: this.email, password: this.password })
      });
      if (!response.ok) {
        this.error = 'Sign-in failed. Check your email and password.';
        return;
      }
      this.session = await response.json() as Session;
      this.password = '';
    } catch {
      this.error = 'The local sign-in service is unavailable.';
    } finally {
      this.loading = false;
    }
  }

  async logout(): Promise<void> {
    this.error = '';
    try {
      const response = await fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' });
      if (!response.ok) {
        this.error = 'Sign-out failed. Please try again.';
        return;
      }
      this.session = null;
    } catch {
      this.error = 'The local sign-in service is unavailable.';
    }
  }
}
bootstrapApplication(AppComponent).catch(console.error);
