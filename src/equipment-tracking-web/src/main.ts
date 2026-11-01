import { bootstrapApplication } from '@angular/platform-browser';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

type Session = { email: string; name: string; roles: string[] };
type Equipment = {
  assetId: string;
  name: string;
  trackingMode: string;
  total: number;
  available: number;
  checkedOut: number;
  damaged: number;
  condition: string;
};

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
        <section aria-labelledby="catalogue-heading">
          <h2 id="catalogue-heading">Equipment catalogue</h2>
          @if (catalogueLoading) { <p>Loading catalogue…</p> }
          @if (catalogueError) { <p class="error" role="alert">{{ catalogueError }}</p> }
          @if (!catalogueLoading && !catalogueError && equipment.length === 0) { <p>No equipment has been added.</p> }
          @if (equipment.length > 0) {
            <ul>
              @for (item of equipment; track item.assetId) {
                <li><strong>{{ item.name }}</strong> ({{ item.assetId }}) —
                  {{ item.trackingMode }}; {{ item.available }}/{{ item.total }} available;
                  {{ item.condition }}</li>
              }
            </ul>
          }
        </section>
        @if (canCreate) {
          <section aria-labelledby="create-heading">
            <h2 id="create-heading">Add equipment</h2>
            <form (ngSubmit)="createEquipment()" #equipmentForm="ngForm">
              <label for="equipment-name">Name</label>
              <input id="equipment-name" name="equipmentName" [(ngModel)]="equipmentName" required>
              <label for="asset-id">Asset ID</label>
              <input id="asset-id" name="assetId" [(ngModel)]="assetId" required>
              <label for="tracking-mode">Tracking mode</label>
              <select id="tracking-mode" name="trackingMode" [(ngModel)]="trackingMode">
                <option value="Individual">Individual</option>
                <option value="Pooled">Pooled</option>
              </select>
              @if (trackingMode === 'Pooled') {
                <label for="total">Total quantity</label>
                <input id="total" name="total" type="number" min="1" step="1" [(ngModel)]="total" required>
              }
              <button type="submit" [disabled]="equipmentForm.invalid || creating">{{ creating ? 'Adding…' : 'Add equipment' }}</button>
            </form>
            @if (createError) { <p class="error" role="alert">{{ createError }}</p> }
          </section>
        }
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
  equipment: Equipment[] = [];
  catalogueLoading = false;
  catalogueError = '';
  equipmentName = '';
  assetId = '';
  trackingMode = 'Individual';
  total: number | null = null;
  creating = false;
  createError = '';

  get canCreate(): boolean {
    return this.session?.roles.some(role => role === 'Office Staff' || role === 'Manager/Admin') ?? false;
  }

  async ngOnInit(): Promise<void> {
    try {
      const response = await fetch('/api/auth/me', { credentials: 'same-origin' });
      if (response.ok) {
        this.session = await response.json() as Session;
        await this.loadCatalogue();
      }
    } catch {
      this.error = 'The local sign-in service is unavailable.';
    }

  }

  async loadCatalogue(): Promise<void> {
    this.catalogueLoading = true;
    this.catalogueError = '';
    try {
      const response = await fetch('/api/equipment', { credentials: 'same-origin' });
      if (!response.ok) {
        this.catalogueError = response.status === 401 ? 'Please sign in again.' : 'Unable to load the equipment catalogue.';
        return;
      }
      this.equipment = await response.json() as Equipment[];
    } catch {
      this.catalogueError = 'The local equipment service is unavailable.';
    } finally {
      this.catalogueLoading = false;
    }
  }

  async createEquipment(): Promise<void> {
    this.createError = '';
    if (!this.equipmentName.trim() || !this.assetId.trim() ||
      (this.trackingMode === 'Pooled' && (!Number.isInteger(this.total) || (this.total ?? 0) <= 0))) {
      this.createError = this.trackingMode === 'Pooled'
        ? 'Enter a name, asset ID, and positive whole-number total.'
        : 'Enter a name and asset ID.';
      return;
    }
    this.creating = true;
    try {
      const response = await fetch('/api/equipment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'same-origin',
        body: JSON.stringify({
          name: this.equipmentName,
          assetId: this.assetId,
          trackingMode: this.trackingMode,
          total: this.trackingMode === 'Pooled' ? this.total : null
        })
      });
      if (!response.ok) {
        const body = await response.json().catch(() => null) as { error?: string } | null;
        this.createError = body?.error ?? 'Unable to add equipment.';
        return;
      }
      this.equipmentName = '';
      this.assetId = '';
      this.total = null;
      await this.loadCatalogue();
    } catch {
      this.createError = 'The local equipment service is unavailable.';
    } finally {
      this.creating = false;
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
      await this.loadCatalogue();
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
      this.equipment = [];
    } catch {
      this.error = 'The local sign-in service is unavailable.';
    }
  }
}
bootstrapApplication(AppComponent).catch(console.error);
