import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, NotificationPreferences } from '../../shared/services/api.service';

@Component({
  selector: 'app-notification-preferences',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="preferences-container">
      <div class="page-header">
        <div>
          <h1>Notification Preferences</h1>
          <p class="subtitle">Choose how you want to be notified about critical intelligence.</p>
        </div>
      </div>

      <form class="preferences-form" (ngSubmit)="savePreferences()">
        <div class="section">
          <h2>Email Notifications</h2>
          <label class="toggle">
            <input
              type="checkbox"
              [(ngModel)]="preferences.emailEnabled"
              name="emailEnabled"
              (change)="handleEmailToggle()"
            />
            <span>Enable email notifications</span>
          </label>

          <div class="field">
            <label>Email address</label>
            <input
              type="email"
              [(ngModel)]="preferences.emailAddress"
              name="emailAddress"
              placeholder="you@company.com"
              [disabled]="!preferences.emailEnabled"
              [ngClass]="{ 'input-error': emailError }"
              (blur)="validateEmail()"
              (input)="onEmailInput()"
            />
            <span class="field-error" *ngIf="emailError">{{ emailError }}</span>
          </div>
        </div>

        <div class="section">
          <h2>Severity Levels</h2>
          <label class="toggle">
            <input type="checkbox" [(ngModel)]="preferences.notifyOnCritical" name="notifyOnCritical" />
            <span>Critical alerts</span>
          </label>
          <label class="toggle">
            <input type="checkbox" [(ngModel)]="preferences.notifyOnHigh" name="notifyOnHigh" />
            <span>High severity alerts</span>
          </label>
          <label class="toggle">
            <input type="checkbox" [(ngModel)]="preferences.notifyOnMedium" name="notifyOnMedium" />
            <span>Medium severity alerts</span>
          </label>
        </div>

        <div class="section">
          <h2>Alert Categories</h2>
          <p class="hint">Comma-separated list of alert types you want to receive.</p>
          <input
            type="text"
            [(ngModel)]="alertTypesInput"
            name="alertTypesInput"
            placeholder="CompetitorMention, TechnologyThreat, RegulatoryChange"
          />
        </div>

        <div class="section">
          <h2>Keyword Watchlist</h2>
          <p class="hint">Comma-separated keywords that should trigger notifications.</p>
          <input
            type="text"
            [(ngModel)]="keywordsInput"
            name="keywordsInput"
            placeholder="semiconductors, supply chain, hydrogen"
          />
        </div>

        <div class="status" *ngIf="statusMessage">
          {{ statusMessage }}
        </div>

        <div class="form-actions">
          <button class="btn-primary" type="submit" [disabled]="isSaving || !!emailError">
            {{ isSaving ? 'Saving...' : 'Save Preferences' }}
          </button>
          <button class="btn-secondary" type="button" (click)="resetDefaults()" [disabled]="isSaving">
            Reset Defaults
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .preferences-container {
      max-width: 900px;
      margin: 0 auto;
      padding: 1.5rem;
    }

    .page-header {
      margin-bottom: 1.5rem;
    }

    .subtitle {
      margin-top: 0.4rem;
      color: var(--text-secondary, #666);
    }

    .preferences-form {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 8px 18px rgba(0, 0, 0, 0.08);
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .section {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .section h2 {
      margin: 0;
      font-size: 1.1rem;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }

    input[type='text'],
    input[type='email'] {
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 0.65rem 0.9rem;
      font-size: 0.95rem;
      background: var(--bg-primary);
      color: var(--text-primary);
    }

    .input-error {
      border-color: #c0392b;
      background: #fff7f5;
    }

    .field-error {
      color: #c0392b;
      font-size: 0.85rem;
    }

    .toggle {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-weight: 500;
    }

    .hint {
      margin: 0;
      color: var(--text-secondary, #666);
      font-size: 0.9rem;
    }

    .status {
      padding: 0.75rem 1rem;
      background: #e9f7ef;
      border-radius: 8px;
      color: #1d7a3f;
      font-weight: 600;
    }

    .form-actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .btn-primary,
    .btn-secondary {
      border: none;
      padding: 0.6rem 1rem;
      border-radius: 8px;
      cursor: pointer;
      font-weight: 600;
    }

    .btn-primary {
      background: var(--primary-color, #1f47ba);
      color: white;
    }

    .btn-secondary {
      background: var(--bg-primary);
      border: 1px solid var(--border-color);
      color: var(--primary-color, #1f47ba);
    }
  `],
})
export class NotificationPreferencesComponent implements OnInit {
  preferences: NotificationPreferences = {
    emailEnabled: true,
    emailAddress: '',
    notifyOnCritical: true,
    notifyOnHigh: true,
    notifyOnMedium: false,
    alertTypesToNotify: [],
    keywordsToNotify: [],
  };

  alertTypesInput = '';
  keywordsInput = '';
  isSaving = false;
  statusMessage = '';
  emailError = '';
  emailTouched = false;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getNotificationPreferences().subscribe({
      next: (prefs) => {
        this.preferences = prefs || this.preferences;
        this.alertTypesInput = this.preferences.alertTypesToNotify.join(', ');
        this.keywordsInput = this.preferences.keywordsToNotify.join(', ');
      },
      error: () => {
        this.statusMessage = 'Unable to load preferences. Please try again later.';
      },
    });
  }

  savePreferences(): void {
    this.emailTouched = true;
    this.validateEmail();
    if (this.emailError) {
      this.statusMessage = 'Please provide a valid email address to enable notifications.';
      return;
    }

    this.isSaving = true;
    this.statusMessage = '';

    const payload: NotificationPreferences = {
      ...this.preferences,
      alertTypesToNotify: this.parseList(this.alertTypesInput),
      keywordsToNotify: this.parseList(this.keywordsInput),
    };

    this.apiService.updateNotificationPreferences(payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.statusMessage = 'Preferences saved successfully.';
      },
      error: () => {
        this.isSaving = false;
        this.statusMessage = 'Failed to save preferences. Please try again.';
      },
    });
  }

  resetDefaults(): void {
    this.preferences = {
      emailEnabled: true,
      emailAddress: this.preferences.emailAddress,
      notifyOnCritical: true,
      notifyOnHigh: true,
      notifyOnMedium: false,
      alertTypesToNotify: [],
      keywordsToNotify: [],
    };
    this.alertTypesInput = '';
    this.keywordsInput = '';
    this.emailError = '';
    this.emailTouched = false;
    this.statusMessage = '';
  }

  handleEmailToggle(): void {
    this.statusMessage = '';
    if (!this.preferences.emailEnabled) {
      this.emailError = '';
      return;
    }

    this.emailTouched = true;
    this.validateEmail();
  }

  onEmailInput(): void {
    this.statusMessage = '';
    if (this.emailTouched) {
      this.validateEmail();
    }
  }

  validateEmail(): void {
    if (!this.preferences.emailEnabled) {
      this.emailError = '';
      return;
    }

    const value = (this.preferences.emailAddress || '').trim();
    if (!value) {
      this.emailError = 'Email address is required when notifications are enabled.';
      return;
    }

    if (!this.isValidEmail(value)) {
      this.emailError = 'Enter a valid email address.';
      return;
    }

    this.emailError = '';
  }

  private isValidEmail(value: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
  }

  private parseList(input: string): string[] {
    return input
      .split(',')
      .map((value) => value.trim())
      .filter((value) => value.length > 0);
  }
}
