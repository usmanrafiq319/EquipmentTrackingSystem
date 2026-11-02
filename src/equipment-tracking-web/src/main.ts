import { bootstrapApplication } from '@angular/platform-browser';
import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

type Session = { email: string; name: string; roles: string[] };
type Equipment = {
  assetId: string; name: string; trackingMode: string; total: number; available: number;
  checkedOut: number; damaged: number; condition: string; isAvailable: boolean | null;
  approvalRequired: boolean; currentCustodian: string | null; dueAt: string | null;
  isOverdue: boolean; hasUnresolvedCustody: boolean; dueStatus: string | null;
  approverEmails: string[];
};
type Custody = {
  id: string; equipmentAssetId: string; equipmentName: string; trackingMode: string;
  borrowerId: string; borrower: string; checkedOutAt: string; dueAt: string;
  declaredLocation: string | null; quantity: number; outstandingQuantity: number;
  isIndividual: boolean; status: string; dueStatus: string | null;
};

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule, DatePipe],
  template: `
    <main>
      <h1>Equipment Tracking</h1>
      @if (session; as current) {
        <p>Signed in as <strong>{{ current.name }}</strong> ({{ current.roles.join(', ') }})</p>
        <button type="button" (click)="logout()">Sign out</button>
        <p class="success" role="status">Your local review session is active.</p>

        <section aria-labelledby="custody-heading">
          <h2 id="custody-heading">Active custody</h2>
          @if (custodyLoading) { <p>Loading active custody…</p> }
          @if (custodyError) { <p class="error" role="alert">{{ custodyError }}</p> }
          @if (!custodyLoading && activeCustody.length === 0) { <p>No active custody records.</p> }
          @for (record of activeCustody; track record.id) {
            <article class="custody-card">
              <h3>{{ record.equipmentName }} ({{ record.equipmentAssetId }})</h3>
              <p><strong>Borrower:</strong> {{ record.borrower }} · <strong>Outstanding:</strong> {{ record.outstandingQuantity }}</p>
              <p><strong>Due:</strong> {{ record.dueAt | date:'medium' }}
                <span [class]="record.dueStatus === 'Overdue' ? 'warning' : 'status'">{{ record.dueStatus }}</span></p>
              @if (record.declaredLocation) { <p><strong>Location:</strong> {{ record.declaredLocation }}</p> }
              <form (ngSubmit)="returnEquipment(record)" #returnForm="ngForm" aria-label="Return equipment">
                @if (record.isIndividual) {
                  <label [for]="'condition-' + record.id">Condition</label>
                  <select [id]="'condition-' + record.id" [name]="'condition-' + record.id" [(ngModel)]="returnCondition[record.id]" required>
                    <option value="Good">Good</option><option value="Damaged">Damaged</option>
                  </select>
                } @else {
                  <label [for]="'return-quantity-' + record.id">Return quantity</label>
                  <input [id]="'return-quantity-' + record.id" [name]="'return-quantity-' + record.id" type="number" min="1" [max]="record.outstandingQuantity" step="1" [(ngModel)]="returnQuantity[record.id]" required>
                  <label [for]="'good-' + record.id">Good quantity</label>
                  <input [id]="'good-' + record.id" [name]="'good-' + record.id" type="number" min="0" step="1" [(ngModel)]="goodQuantity[record.id]" required>
                  <label [for]="'damaged-' + record.id">Damaged quantity</label>
                  <input [id]="'damaged-' + record.id" [name]="'damaged-' + record.id" type="number" min="0" step="1" [(ngModel)]="damagedQuantity[record.id]" required>
                }
                <label [for]="'return-note-' + record.id">Note (optional)</label>
                <textarea [id]="'return-note-' + record.id" [name]="'return-note-' + record.id" maxlength="2000" [(ngModel)]="returnNote[record.id]"></textarea>
                <button type="submit" [disabled]="returningId === record.id || returnForm.invalid">{{ returningId === record.id ? 'Returning…' : 'Return equipment' }}</button>
              </form>
            </article>
          }
        </section>

        <section aria-labelledby="catalogue-heading">
          <h2 id="catalogue-heading">Equipment catalogue</h2>
          <form class="search" (ngSubmit)="searchCatalogue()">
            <label for="catalogue-search">Search by name or asset ID</label>
            <div class="search-row"><input id="catalogue-search" name="search" [(ngModel)]="search" autocomplete="off"><button type="submit" [disabled]="catalogueLoading">Search</button>
              @if (search) { <button type="button" (click)="clearSearch()">Clear</button> }</div>
          </form>
          @if (catalogueLoading) { <p>Loading catalogue…</p> }
          @if (catalogueError) { <p class="error" role="alert">{{ catalogueError }}</p> }
          @if (!catalogueLoading && !catalogueError && equipment.length === 0) { <p>No equipment matches your search.</p> }
          @if (equipment.length > 0) {
            <ul class="catalogue">@for (item of equipment; track item.assetId) {
              <li><button type="button" class="equipment-link" (click)="selectEquipment(item)"><strong>{{ item.name }}</strong> ({{ item.assetId }})</button></li>
            }</ul>
          }
          @if (selectedEquipment; as item) {
            <article class="detail" aria-labelledby="detail-heading">
              <h3 id="detail-heading">{{ item.name }}</h3>
              <p><strong>Asset ID:</strong> {{ item.assetId }} · <strong>Tracking:</strong> {{ item.trackingMode }}</p>
              @if (item.trackingMode === 'Pooled') {
                <p><strong>Total:</strong> {{ item.total }} · <strong>Available:</strong> {{ item.available }} · <strong>Checked-out:</strong> {{ item.checkedOut }} · <strong>Damaged:</strong> {{ item.damaged }}</p>
              } @else {
                <p><strong>Availability:</strong> {{ item.isAvailable ? 'Available' : 'Unavailable' }} · <strong>Condition:</strong> {{ item.condition }}</p>
                <p><strong>Current custodian:</strong> {{ item.currentCustodian ?? 'None recorded' }} · <strong>Due:</strong> {{ item.dueAt ?? 'None recorded' }}
                  @if (item.dueStatus) { <span [class]="item.dueStatus === 'Overdue' ? 'warning' : 'status'">({{ item.dueStatus }})</span> }</p>
              }
              <p><strong>Approval:</strong> {{ item.approvalRequired ? 'Restricted' : 'Not restricted' }}</p>
              @if (item.approvalRequired) { <p class="notice">Restricted equipment requires an approved request.</p> }
              @if (item.isAvailable || (item.trackingMode === 'Pooled' && item.available > 0)) {
                <form (ngSubmit)="checkout(item)" #checkoutForm="ngForm" aria-label="Check out equipment">
                  @if (item.trackingMode === 'Pooled') {
                    <label [for]="'checkout-quantity-' + item.assetId">Quantity</label>
                    <input [id]="'checkout-quantity-' + item.assetId" name="checkoutQuantity" type="number" min="1" [max]="item.available" step="1" [(ngModel)]="checkoutQuantity" required>
                  }
                  <label [for]="'location-' + item.assetId">Location (optional)</label>
                  <input [id]="'location-' + item.assetId" name="location" maxlength="200" [(ngModel)]="declaredLocation">
                  <button type="submit" [disabled]="checkingOut || checkoutForm.invalid">{{ checkingOut ? 'Checking out…' : 'Check out' }}</button>
                </form>
              }
            </article>
          }
        </section>
        @if (workflowMessage) { <p class="success" role="status">{{ workflowMessage }}</p> }
        @if (workflowError) { <p class="error" role="alert">{{ workflowError }}</p> }
        @if (canCreate) {
          <section aria-labelledby="create-heading"><h2 id="create-heading">Add equipment</h2>
            <form (ngSubmit)="createEquipment()" #equipmentForm="ngForm">
              <label for="equipment-name">Name</label><input id="equipment-name" name="equipmentName" [(ngModel)]="equipmentName" required>
              <label for="asset-id">Asset ID</label><input id="asset-id" name="assetId" [(ngModel)]="assetId" required>
              <label for="tracking-mode">Tracking mode</label><select id="tracking-mode" name="trackingMode" [(ngModel)]="trackingMode"><option>Individual</option><option>Pooled</option></select>
              @if (trackingMode === 'Pooled') { <label for="total">Total quantity</label><input id="total" name="total" type="number" min="1" step="1" [(ngModel)]="total" required> }
              <button type="submit" [disabled]="equipmentForm.invalid || creating">{{ creating ? 'Adding…' : 'Add equipment' }}</button>
            </form>
            @if (createError) { <p class="error" role="alert">{{ createError }}</p> }
          </section>
        }
      } @else {
        <form (ngSubmit)="login()" #loginForm="ngForm"><label for="email">Email</label><input id="email" name="email" type="email" [(ngModel)]="email" required autocomplete="username"><label for="password">Password</label><input id="password" name="password" type="password" [(ngModel)]="password" required autocomplete="current-password"><button type="submit" [disabled]="loginForm.invalid || loading">{{ loading ? 'Signing in…' : 'Sign in' }}</button></form>
        @if (error) { <p class="error" role="alert">{{ error }}</p> }
      }
    </main>
  `
})
class AppComponent implements OnInit {
  email = ''; password = ''; session: Session | null = null; error = ''; loading = false;
  equipment: Equipment[] = []; selectedEquipment: Equipment | null = null; search = ''; catalogueLoading = false; catalogueError = '';
  activeCustody: Custody[] = []; custodyLoading = false; custodyError = '';
  equipmentName = ''; assetId = ''; trackingMode = 'Individual'; total: number | null = null; creating = false; createError = '';
  checkoutQuantity = 1; declaredLocation = ''; checkingOut = false; returningId: string | null = null;
  returnCondition: Record<string, string> = {}; returnQuantity: Record<string, number> = {}; goodQuantity: Record<string, number> = {}; damagedQuantity: Record<string, number> = {}; returnNote: Record<string, string> = {};
  workflowMessage = ''; workflowError = '';
  get canCreate(): boolean { return this.session?.roles.some(role => role === 'Office Staff' || role === 'Manager/Admin') ?? false; }

  async ngOnInit(): Promise<void> {
    try { const response = await fetch('/api/auth/me', { credentials: 'same-origin' }); if (response.ok) { this.session = await response.json() as Session; await this.refreshState(); } }
    catch { this.error = 'The local sign-in service is unavailable.'; }
  }
  async refreshState(): Promise<void> { await Promise.all([this.loadCatalogue(), this.loadCustody()]); }
  async loadCustody(): Promise<void> {
    this.custodyLoading = true; this.custodyError = '';
    try { const response = await fetch('/api/custody/active', { credentials: 'same-origin' }); if (!response.ok) { this.custodyError = response.status === 401 ? 'Please sign in again.' : 'Unable to load active custody.'; return; } this.activeCustody = await response.json() as Custody[]; }
    catch { this.activeCustody = []; this.custodyError = 'The local equipment service is unavailable.'; } finally { this.custodyLoading = false; }
  }
  async loadCatalogue(): Promise<void> {
    this.catalogueLoading = true; this.catalogueError = '';
    try { const query = this.search.trim() ? `?search=${encodeURIComponent(this.search.trim())}` : ''; const response = await fetch(`/api/equipment${query}`, { credentials: 'same-origin' }); if (!response.ok) { if (response.status === 401) this.session = null; this.catalogueError = response.status === 401 ? 'Please sign in again.' : 'Unable to load the equipment catalogue.'; return; } this.equipment = await response.json() as Equipment[]; this.selectedEquipment = null; }
    catch { this.equipment = []; this.selectedEquipment = null; this.catalogueError = 'The local equipment service is unavailable.'; } finally { this.catalogueLoading = false; }
  }
  searchCatalogue(): Promise<void> { return this.loadCatalogue(); }
  clearSearch(): void { this.search = ''; void this.loadCatalogue(); }
  selectEquipment(item: Equipment): void { this.selectedEquipment = item; this.checkoutQuantity = item.trackingMode === 'Pooled' ? 1 : 1; this.workflowError = ''; }
  private async responseError(response: Response, fallback: string): Promise<string> { const body = await response.json().catch(() => null) as { error?: string } | null; return body?.error ?? (response.status === 401 ? 'Please sign in again.' : response.status === 403 ? 'You are not authorized for this action.' : response.status === 409 ? 'The record changed. The latest state has been loaded; try again.' : fallback); }
  async checkout(item: Equipment): Promise<void> {
    this.workflowMessage = ''; this.workflowError = ''; this.checkingOut = true;
    try { const response = await fetch('/api/custody/checkouts', { method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin', body: JSON.stringify({ equipmentAssetId: item.assetId, quantity: item.trackingMode === 'Pooled' ? this.checkoutQuantity : null, declaredLocation: this.declaredLocation }) }); if (!response.ok) { this.workflowError = await this.responseError(response, 'Unable to check out equipment.'); if (response.status === 409) await this.refreshState(); return; } this.workflowMessage = 'Equipment checked out successfully.'; this.declaredLocation = ''; this.checkoutQuantity = 1; await this.refreshState(); }
    catch { this.workflowError = 'The equipment service is unavailable. No checkout was recorded.'; } finally { this.checkingOut = false; }
  }
  async returnEquipment(record: Custody): Promise<void> {
    this.workflowMessage = ''; this.workflowError = ''; this.returningId = record.id;
    const quantity = record.isIndividual ? 1 : this.returnQuantity[record.id]; const good = record.isIndividual ? undefined : (this.goodQuantity[record.id] ?? -1); const damaged = record.isIndividual ? undefined : (this.damagedQuantity[record.id] ?? -1);
    if (!record.isIndividual && (!Number.isInteger(quantity) || quantity <= 0 || !Number.isInteger(good) || !Number.isInteger(damaged) || (good ?? -1) < 0 || (damaged ?? -1) < 0 || (good ?? -1) + (damaged ?? -1) !== quantity || quantity > record.outstandingQuantity)) { this.workflowError = 'Return quantity must be positive, whole, within outstanding custody, and equal Good plus Damaged.'; this.returningId = null; return; }
    try { const response = await fetch(`/api/custody/${record.id}/returns`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin', body: JSON.stringify({ quantity: record.isIndividual ? null : quantity, condition: record.isIndividual ? this.returnCondition[record.id] ?? 'Good' : undefined, goodQuantity: good, damagedQuantity: damaged, note: this.returnNote[record.id] }) }); if (!response.ok) { this.workflowError = await this.responseError(response, 'Unable to return equipment.'); if (response.status === 409) await this.refreshState(); return; } this.workflowMessage = 'Equipment returned successfully.'; await this.refreshState(); }
    catch { this.workflowError = 'The equipment service is unavailable. No return was recorded.'; } finally { this.returningId = null; }
  }
  async createEquipment(): Promise<void> {
    this.createError = ''; if (!this.equipmentName.trim() || !this.assetId.trim() || (this.trackingMode === 'Pooled' && (!Number.isInteger(this.total) || (this.total ?? 0) <= 0))) { this.createError = 'Enter valid equipment details.'; return; } this.creating = true;
    try { const response = await fetch('/api/equipment', { method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin', body: JSON.stringify({ name: this.equipmentName, assetId: this.assetId, trackingMode: this.trackingMode, total: this.trackingMode === 'Pooled' ? this.total : null }) }); if (!response.ok) { this.createError = await this.responseError(response, 'Unable to add equipment.'); return; } this.equipmentName = ''; this.assetId = ''; this.total = null; await this.loadCatalogue(); } catch { this.createError = 'The local equipment service is unavailable.'; } finally { this.creating = false; }
  }
  async login(): Promise<void> { this.error = ''; this.loading = true; try { const response = await fetch('/api/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin', body: JSON.stringify({ email: this.email, password: this.password }) }); if (!response.ok) { this.error = 'Sign-in failed. Check your email and password.'; return; } this.session = await response.json() as Session; this.password = ''; await this.refreshState(); } catch { this.error = 'The local sign-in service is unavailable.'; } finally { this.loading = false; } }
  async logout(): Promise<void> { try { const response = await fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' }); if (!response.ok) { this.error = 'Sign-out failed. Please try again.'; return; } this.session = null; this.equipment = []; this.activeCustody = []; this.selectedEquipment = null; } catch { this.error = 'The local equipment service is unavailable.'; } }
}
bootstrapApplication(AppComponent).catch(console.error);
