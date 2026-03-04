import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ApiService,
  TenderNotice,
  TenderIngestionRun,
  TenderNotificationRule,
  CreateTenderNotificationRule,
  TenderSource,
  CreateTenderSource,
  UpdateTenderSourceRollout,
  TenderRolloutSummary
} from '../../shared/services/api.service';

@Component({
  selector: 'app-tender-monitoring',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="tender-page">
      <header class="hero">
        <h2>Government Tender Monitoring</h2>
        <p>Track Saudi and Middle East tenders with freshness, status, and source visibility.</p>
      </header>

      <div class="tabs">
        <button type="button" [class.active]="activeTab === 'saudi'" (click)="activeTab = 'saudi'">Saudi</button>
        <button type="button" [class.active]="activeTab === 'middleEast'" (click)="activeTab = 'middleEast'">Middle East</button>
        <button type="button" [class.active]="activeTab === 'sources'" (click)="activeTab = 'sources'">Sources</button>
        <button type="button" [class.active]="activeTab === 'rules'" (click)="activeTab = 'rules'">Rules</button>
        <button type="button" [class.active]="activeTab === 'ops'" (click)="activeTab = 'ops'">Ops</button>
      </div>

      <section class="panel" *ngIf="activeTab === 'saudi'">
        <h3>Saudi Tenders</h3>
        <div class="loading" *ngIf="loadingSaudi">Loading Saudi tenders...</div>
        <div class="error" *ngIf="errorSaudi">{{ errorSaudi }}</div>

        <div class="table-wrap" *ngIf="!loadingSaudi && !errorSaudi">
          <table>
            <thead>
              <tr>
                <th>Title</th>
                <th>Authority</th>
                <th>Sector</th>
                <th>Publish</th>
                <th>Deadline</th>
                <th>Status</th>
                <th>Source</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of saudiTenders">
                <td>
                  <strong>{{ item.title }}</strong>
                  <div class="sub">v{{ item.currentVersionNo }} • {{ item.countryIsoCode }}</div>
                </td>
                <td>{{ item.authorityName || '—' }}</td>
                <td>{{ item.sector || '—' }}</td>
                <td>{{ item.publishDate ? (item.publishDate | date: 'yyyy-MM-dd') : '—' }}</td>
                <td>{{ item.deadline ? (item.deadline | date: 'yyyy-MM-dd') : '—' }}</td>
                <td><span class="pill">{{ item.status }}</span></td>
                <td><a [href]="item.sourceUrl" target="_blank">Open</a></td>
              </tr>
              <tr *ngIf="!saudiTenders.length">
                <td colspan="7" class="empty">No Saudi tenders found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel" *ngIf="activeTab === 'middleEast'">
        <h3>Middle East Tenders</h3>
        <div class="loading" *ngIf="loadingMiddleEast">Loading Middle East tenders...</div>
        <div class="error" *ngIf="errorMiddleEast">{{ errorMiddleEast }}</div>

        <div class="table-wrap" *ngIf="!loadingMiddleEast && !errorMiddleEast">
          <table>
            <thead>
              <tr>
                <th>Title</th>
                <th>Country</th>
                <th>Authority</th>
                <th>Publish</th>
                <th>Deadline</th>
                <th>Status</th>
                <th>Source</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of middleEastTenders">
                <td>
                  <strong>{{ item.title }}</strong>
                  <div class="sub">v{{ item.currentVersionNo }} • {{ item.countryIsoCode }}</div>
                </td>
                <td>{{ item.countryName }}</td>
                <td>{{ item.authorityName || '—' }}</td>
                <td>{{ item.publishDate ? (item.publishDate | date: 'yyyy-MM-dd') : '—' }}</td>
                <td>{{ item.deadline ? (item.deadline | date: 'yyyy-MM-dd') : '—' }}</td>
                <td><span class="pill">{{ item.status }}</span></td>
                <td><a [href]="item.sourceUrl" target="_blank">Open</a></td>
              </tr>
              <tr *ngIf="!middleEastTenders.length">
                <td colspan="7" class="empty">No Middle East tenders found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel" *ngIf="activeTab === 'sources'">
        <h3>Tender Sources</h3>

        <div class="rollout-panel">
          <div class="rollout-title">Canary Rollout</div>
          <div class="rollout-stats" *ngIf="rolloutSummary">
            <span>Total: {{ rolloutSummary.totalSources }}</span>
            <span>Canary: {{ rolloutSummary.canaryCount }}</span>
            <span>Pilot: {{ rolloutSummary.pilotCount }}</span>
            <span>General: {{ rolloutSummary.generalCount }}</span>
            <span>Disabled: {{ rolloutSummary.disabledCount }}</span>
          </div>
          <div class="rollout-actions">
            <button type="button" class="secondary" (click)="promoteAll('Canary', 'Pilot')">Promote Canary → Pilot</button>
            <button type="button" class="secondary" (click)="promoteAll('Pilot', 'General')">Promote Pilot → General</button>
          </div>
          <div class="loading" *ngIf="loadingRollout">Updating rollout...</div>
          <div class="error" *ngIf="errorRollout">{{ errorRollout }}</div>
        </div>

        <form class="rule-form" (ngSubmit)="saveSource()">
          <div class="row">
            <label>
              Name
              <input type="text" [(ngModel)]="sourceForm.name" name="sourceName" required />
            </label>

            <label>
              Type
              <select [(ngModel)]="sourceForm.type" name="sourceType">
                <option value="API">API</option>
                <option value="RSS">RSS</option>
                <option value="Crawler">Crawler</option>
              </select>
            </label>

            <label>
              Base Url
              <input type="url" [(ngModel)]="sourceForm.baseUrl" name="sourceBaseUrl" required />
            </label>
          </div>

          <div class="row">
            <label>
              Auth Mode
              <input type="text" [(ngModel)]="sourceForm.authMode" name="sourceAuthMode" placeholder="None, Key, OAuth" />
            </label>

            <label>
              Poll Priority
              <input type="number" [(ngModel)]="sourceForm.pollPriority" name="sourcePollPriority" />
            </label>

            <label>
              Poll Interval (min)
              <input type="number" [(ngModel)]="sourceForm.pollIntervalMin" name="sourcePollIntervalMin" />
            </label>

            <label>
              Rollout Stage
              <select [(ngModel)]="sourceForm.rolloutStage" name="sourceRolloutStage">
                <option value="Disabled">Disabled</option>
                <option value="Canary">Canary</option>
                <option value="Pilot">Pilot</option>
                <option value="General">General</option>
              </select>
            </label>
          </div>

          <div class="row">
            <label>
              Owner
              <input type="text" [(ngModel)]="sourceForm.owner" name="sourceOwner" placeholder="team or person" />
            </label>

            <label>
              Legal Notes
              <input type="text" [(ngModel)]="sourceForm.legalNotes" name="sourceLegalNotes" placeholder="optional" />
            </label>

            <label>
              Rate Limit Policy JSON
              <input type="text" [(ngModel)]="sourceForm.rateLimitPolicyJson" name="sourceRatePolicy" placeholder="optional" />
            </label>
          </div>

          <div class="row">
            <label class="full-width">
              Connector Config JSON
              <textarea
                rows="4"
                [(ngModel)]="sourceForm.connectorConfigJson"
                name="sourceConnectorConfig"
                placeholder='{"connector":"api-json","list_path":"data.items","field_map":{"title":"title"}}'
              ></textarea>
            </label>
          </div>

          <div class="row compact">
            <label class="checkbox">
              <input type="checkbox" [(ngModel)]="sourceForm.isEnabled" name="sourceIsEnabled" />
              Enabled
            </label>

            <label class="checkbox">
              <input type="checkbox" [(ngModel)]="sourceForm.isCanary" name="sourceIsCanary" />
              Canary Source
            </label>

            <div class="form-actions">
              <button type="submit">{{ editingSourceId ? 'Update Source' : 'Create Source' }}</button>
              <button type="button" class="secondary" *ngIf="editingSourceId" (click)="cancelSourceEdit()">Cancel</button>
            </div>
          </div>
        </form>

        <div class="loading" *ngIf="loadingSources">Loading sources...</div>
        <div class="error" *ngIf="errorSources">{{ errorSources }}</div>

        <div class="table-wrap" *ngIf="!loadingSources && !errorSources">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Type</th>
                <th>Base Url</th>
                <th>Priority</th>
                <th>Interval</th>
                <th>Stage</th>
                <th>Status</th>
                <th>Owner</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let source of sources">
                <td>{{ source.name }}</td>
                <td>{{ source.type }}</td>
                <td><a [href]="source.baseUrl" target="_blank">{{ source.baseUrl }}</a></td>
                <td>{{ source.pollPriority }}</td>
                <td>{{ source.pollIntervalMin }} min</td>
                <td>{{ source.rolloutStage || 'General' }}</td>
                <td>
                  <span class="pill" [class.danger]="!source.isEnabled">{{ source.isEnabled ? 'Enabled' : 'Disabled' }}</span>
                </td>
                <td>{{ source.owner || '—' }}</td>
                <td>
                  <button type="button" class="link" (click)="editSource(source)">Edit</button>
                  <button type="button" class="link" (click)="toggleSourceStatus(source)">{{ source.isEnabled ? 'Disable' : 'Enable' }}</button>
                  <button type="button" class="link" (click)="setRolloutStage(source, 'Canary')">Canary</button>
                  <button type="button" class="link" (click)="setRolloutStage(source, 'Pilot')">Pilot</button>
                  <button type="button" class="link" (click)="setRolloutStage(source, 'General')">General</button>
                  <button type="button" class="link danger" (click)="deleteSource(source.id)">Delete</button>
                </td>
              </tr>
              <tr *ngIf="!sources.length">
                <td colspan="9" class="empty">No tender sources found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel" *ngIf="activeTab === 'rules'">
        <h3>Tender Notification Rules</h3>

        <form class="rule-form" (ngSubmit)="saveRule()">
          <div class="row">
            <label>
              Scope
              <select [(ngModel)]="ruleForm.scope" name="scope">
                <option value="Global">Global</option>
                <option value="User">User</option>
              </select>
            </label>

            <label>
              Channels
              <input type="text" [(ngModel)]="ruleForm.channels" name="channels" placeholder="InApp,Email" />
            </label>

            <label>
              User Id
              <input type="text" [(ngModel)]="ruleForm.userId" name="userId" placeholder="optional" />
            </label>
          </div>

          <div class="row">
            <label>
              Country Filter
              <input type="text" [(ngModel)]="ruleForm.countryFilter" name="countryFilter" placeholder="SA,AE" />
            </label>

            <label>
              Sector Filter
              <input type="text" [(ngModel)]="ruleForm.sectorFilter" name="sectorFilter" placeholder="Power Grid" />
            </label>

            <label>
              Authority Filter
              <input type="text" [(ngModel)]="ruleForm.authorityFilter" name="authorityFilter" placeholder="SEC" />
            </label>
          </div>

          <div class="row">
            <label>
              Value Min
              <input type="number" [(ngModel)]="ruleForm.valueMin" name="valueMin" />
            </label>

            <label>
              Value Max
              <input type="number" [(ngModel)]="ruleForm.valueMax" name="valueMax" />
            </label>

            <label>
              Keywords
              <input type="text" [(ngModel)]="ruleForm.keywords" name="keywords" placeholder="substation,transformer" />
            </label>
          </div>

          <div class="row compact">
            <label class="checkbox">
              <input type="checkbox" [(ngModel)]="ruleForm.isActive" name="isActive" />
              Active
            </label>

            <div class="form-actions">
              <button type="submit">{{ editingRuleId ? 'Update Rule' : 'Create Rule' }}</button>
              <button type="button" class="secondary" *ngIf="editingRuleId" (click)="cancelEdit()">Cancel</button>
            </div>
          </div>
        </form>

        <div class="loading" *ngIf="loadingRules">Loading rules...</div>
        <div class="error" *ngIf="errorRules">{{ errorRules }}</div>

        <div class="table-wrap" *ngIf="!loadingRules && !errorRules">
          <div class="rules-toolbar">
            <input
              type="text"
              [(ngModel)]="ruleSearch"
              name="ruleSearch"
              placeholder="Search by country, sector, authority, keywords"
            />
            <select [(ngModel)]="ruleStatusFilter" name="ruleStatusFilter">
              <option value="all">All</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>

          <table>
            <thead>
              <tr>
                <th>Scope</th>
                <th>Channels</th>
                <th>Country</th>
                <th>Sector</th>
                <th>Authority</th>
                <th>Keywords</th>
                <th>Range</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let rule of filteredRules">
                <td>{{ rule.scope }}</td>
                <td>{{ rule.channels }}</td>
                <td>{{ rule.countryFilter || '—' }}</td>
                <td>{{ rule.sectorFilter || '—' }}</td>
                <td>{{ rule.authorityFilter || '—' }}</td>
                <td>{{ rule.keywords || '—' }}</td>
                <td>{{ rule.valueMin ?? '—' }} - {{ rule.valueMax ?? '—' }}</td>
                <td>
                  <button type="button" class="link" (click)="editRule(rule)">Edit</button>
                  <button type="button" class="link danger" (click)="deleteRule(rule.id)">Delete</button>
                </td>
              </tr>
              <tr *ngIf="!filteredRules.length">
                <td colspan="8" class="empty">No active rules found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel" *ngIf="activeTab === 'ops'">
        <h3>Failed Ingestion Runs</h3>
        <div class="loading" *ngIf="loadingOps">Loading failed runs...</div>
        <div class="error" *ngIf="errorOps">{{ errorOps }}</div>

        <div class="table-wrap" *ngIf="!loadingOps && !errorOps">
          <table>
            <thead>
              <tr>
                <th>Source</th>
                <th>Started</th>
                <th>Status</th>
                <th>Fetched</th>
                <th>New</th>
                <th>Updated</th>
                <th>Errors</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let run of failedRuns">
                <td>{{ run.sourceName }}</td>
                <td>{{ run.startedAt | date: 'yyyy-MM-dd HH:mm' }}</td>
                <td><span class="pill danger">{{ run.status }}</span></td>
                <td>{{ run.itemsFetched }}</td>
                <td>{{ run.itemsNew }}</td>
                <td>{{ run.itemsUpdated }}</td>
                <td class="error-cell">{{ run.errors || '—' }}</td>
              </tr>
              <tr *ngIf="!failedRuns.length">
                <td colspan="7" class="empty">No failed runs found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </section>
  `,
  styles: [`
    :host { display: block; }

    .tender-page {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .hero h2 {
      margin: 0;
      font-size: 1.6rem;
      color: var(--text-primary, #111827);
    }

    .hero p {
      margin: 0.25rem 0 0;
      color: var(--text-secondary, #4b5563);
    }

    .tabs {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .tabs button {
      border: 1px solid var(--border-color, #d1d5db);
      background: var(--bg-secondary, #f9fafb);
      color: var(--text-primary, #111827);
      padding: 0.5rem 0.75rem;
      border-radius: 6px;
      cursor: pointer;
    }

    .tabs button.active {
      background: var(--primary-color, #1f47ba);
      color: #fff;
      border-color: var(--primary-color, #1f47ba);
    }

    .panel {
      background: var(--bg-secondary, #f9fafb);
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 10px;
      padding: 1rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
    }

    .panel h3 {
      margin: 0 0 0.75rem;
    }

    .table-wrap {
      overflow: auto;
    }

    .rollout-panel {
      margin-bottom: 0.9rem;
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 8px;
      padding: 0.65rem 0.75rem;
      background: var(--bg-secondary, #f9fafb);
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .rollout-title {
      font-weight: 600;
      color: var(--text-primary, #111827);
    }

    .rollout-stats {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
      font-size: 0.85rem;
      color: var(--text-secondary, #4b5563);
    }

    .rollout-actions {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .rules-toolbar {
      display: flex;
      gap: 0.5rem;
      margin-bottom: 0.6rem;
      align-items: center;
    }

    .rules-toolbar input,
    .rules-toolbar select {
      padding: 0.45rem 0.55rem;
      border: 1px solid var(--border-color, #d1d5db);
      border-radius: 6px;
      background: var(--bg-primary, #ffffff);
      color: var(--text-primary, #111827);
    }

    .rules-toolbar input {
      flex: 1;
    }

    .rule-form {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      margin-bottom: 1rem;
      padding: 0.75rem;
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 8px;
      background: var(--bg-secondary, #f9fafb);
    }

    .rule-form .row {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 0.75rem;
    }

    .rule-form .row.compact {
      grid-template-columns: 1fr auto;
      align-items: center;
    }

    .rule-form label {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      font-size: 0.85rem;
      color: var(--text-secondary, #4b5563);
    }

    .rule-form input,
    .rule-form select,
    .rule-form textarea {
      padding: 0.45rem 0.55rem;
      border: 1px solid var(--border-color, #d1d5db);
      border-radius: 6px;
      background: var(--bg-primary, #ffffff);
      color: var(--text-primary, #111827);
    }

    .full-width {
      grid-column: 1 / -1;
    }

    .checkbox {
      flex-direction: row !important;
      align-items: center;
      gap: 0.5rem !important;
    }

    .form-actions {
      display: flex;
      gap: 0.5rem;
    }

    .form-actions button {
      padding: 0.45rem 0.8rem;
      border: 1px solid var(--primary-color, #1f47ba);
      background: var(--primary-color, #1f47ba);
      color: #fff;
      border-radius: 6px;
      cursor: pointer;
    }

    .form-actions button.secondary {
      background: transparent;
      color: var(--primary-color, #1f47ba);
    }

    .link {
      background: transparent;
      border: none;
      color: var(--primary-color, #1f47ba);
      cursor: pointer;
      margin-right: 0.5rem;
      padding: 0;
    }

    .link.danger {
      color: #b91c1c;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      min-width: 900px;
    }

    th, td {
      text-align: left;
      padding: 0.6rem;
      border-bottom: 1px solid var(--border-color, #e5e7eb);
      font-size: 0.9rem;
      vertical-align: top;
    }

    th {
      color: var(--text-secondary, #4b5563);
      font-weight: 600;
      background: var(--bg-secondary, #f9fafb);
    }

    .sub {
      font-size: 0.75rem;
      color: var(--text-secondary, #6b7280);
      margin-top: 0.2rem;
    }

    .pill {
      display: inline-block;
      padding: 0.2rem 0.5rem;
      border-radius: 999px;
      background: transparent;
      color: var(--primary-color, #1f47ba);
      border: 1px solid var(--primary-color, #1f47ba);
      font-size: 0.75rem;
      font-weight: 600;
    }

    .pill.danger {
      background: transparent;
      color: var(--danger-color, #b91c1c);
      border-color: var(--danger-color, #b91c1c);
    }

    .loading {
      color: var(--text-secondary, #4b5563);
      margin: 0.5rem 0;
    }

    .error {
      color: #b91c1c;
      margin: 0.5rem 0;
    }

    .empty {
      text-align: center;
      color: var(--text-secondary, #6b7280);
      padding: 1rem;
    }

    .error-cell {
      color: #b91c1c;
      max-width: 420px;
      white-space: pre-wrap;
    }

    a {
      color: var(--primary-color, #1f47ba);
      text-decoration: none;
    }

    a:hover {
      text-decoration: underline;
    }

    :host-context(.dark-theme) .panel {
      box-shadow: none;
    }

    :host-context(.dark-theme) .tabs button.active {
      color: var(--text-primary, #f0f0f0);
    }

    :host-context(.dark-theme) .rule-form input,
    :host-context(.dark-theme) .rule-form select,
    :host-context(.dark-theme) .rule-form textarea,
    :host-context(.dark-theme) .rules-toolbar input,
    :host-context(.dark-theme) .rules-toolbar select {
      border-color: var(--border-color, #444444);
    }

    :host-context(.dark-theme) th {
      color: var(--text-primary, #f0f0f0);
    }

    :host-context(.dark-theme) .pill,
    :host-context(.dark-theme) .pill.danger {
      background: var(--bg-primary, #1a1a1a);
    }
  `]
})
export class TenderMonitoringComponent implements OnInit {
  activeTab: 'saudi' | 'middleEast' | 'sources' | 'rules' | 'ops' = 'saudi';

  saudiTenders: TenderNotice[] = [];
  middleEastTenders: TenderNotice[] = [];
  failedRuns: TenderIngestionRun[] = [];
  rules: TenderNotificationRule[] = [];
  sources: TenderSource[] = [];

  loadingSaudi = false;
  loadingMiddleEast = false;
  loadingOps = false;
  loadingRules = false;
  loadingSources = false;
  loadingRollout = false;

  errorSaudi = '';
  errorMiddleEast = '';
  errorOps = '';
  errorRules = '';
  errorSources = '';
  errorRollout = '';
  ruleSearch = '';
  ruleStatusFilter: 'all' | 'active' | 'inactive' = 'all';

  editingRuleId: string | null = null;
  ruleForm: CreateTenderNotificationRule = {
    scope: 'Global',
    channels: 'InApp',
    userId: undefined,
    countryFilter: undefined,
    sectorFilter: undefined,
    authorityFilter: undefined,
    valueMin: undefined,
    valueMax: undefined,
    keywords: undefined,
    isActive: true
  };

  editingSourceId: string | null = null;
  rolloutSummary: TenderRolloutSummary | null = null;
  sourceForm: CreateTenderSource = {
    name: '',
    type: 'API',
    baseUrl: '',
    authMode: undefined,
    pollPriority: 100,
    pollIntervalMin: 60,
    rateLimitPolicyJson: undefined,
    connectorConfigJson: undefined,
    isCanary: false,
    rolloutStage: 'General',
    isEnabled: true,
    legalNotes: undefined,
    owner: undefined
  };

  constructor(private readonly apiService: ApiService) {}

  ngOnInit(): void {
    this.loadSaudi();
    this.loadMiddleEast();
    this.loadSources();
    this.loadRules();
    this.loadFailedRuns();
  }

  private loadSources(): void {
    this.loadingSources = true;
    this.errorSources = '';

    this.apiService.getTenderSources(true).subscribe({
      next: (data) => {
        this.sources = data || [];
        this.loadingSources = false;
        this.loadRolloutSummary();
      },
      error: (err) => {
        this.errorSources = err?.message || 'Failed to load tender sources';
        this.loadingSources = false;
      }
    });
  }

  private loadRolloutSummary(): void {
    this.errorRollout = '';
    this.apiService.getTenderRolloutSummary().subscribe({
      next: (data) => {
        this.rolloutSummary = data;
      },
      error: (err) => {
        this.errorRollout = err?.message || 'Failed to load rollout summary';
      }
    });
  }

  private loadRules(): void {
    this.loadingRules = true;
    this.errorRules = '';

    this.apiService.getTenderRules().subscribe({
      next: (data) => {
        this.rules = data || [];
        this.loadingRules = false;
      },
      error: (err) => {
        this.errorRules = err?.message || 'Failed to load tender rules';
        this.loadingRules = false;
      }
    });
  }

  private loadSaudi(): void {
    this.loadingSaudi = true;
    this.errorSaudi = '';

    this.apiService.getSaudiTenders(1, 100).subscribe({
      next: (data) => {
        this.saudiTenders = data || [];
        this.loadingSaudi = false;
      },
      error: (err) => {
        this.errorSaudi = err?.message || 'Failed to load Saudi tenders';
        this.loadingSaudi = false;
      }
    });
  }

  private loadMiddleEast(): void {
    this.loadingMiddleEast = true;
    this.errorMiddleEast = '';

    this.apiService.getMiddleEastTenders(1, 100).subscribe({
      next: (data) => {
        this.middleEastTenders = data || [];
        this.loadingMiddleEast = false;
      },
      error: (err) => {
        this.errorMiddleEast = err?.message || 'Failed to load Middle East tenders';
        this.loadingMiddleEast = false;
      }
    });
  }

  private loadFailedRuns(): void {
    this.loadingOps = true;
    this.errorOps = '';

    this.apiService.getFailedTenderRuns(100).subscribe({
      next: (data) => {
        this.failedRuns = data || [];
        this.loadingOps = false;
      },
      error: (err) => {
        this.errorOps = err?.message || 'Failed to load failed runs';
        this.loadingOps = false;
      }
    });
  }

  saveSource(): void {
    if (!this.sourceForm.name?.trim() || !this.sourceForm.baseUrl?.trim()) {
      this.errorSources = 'Name and Base Url are required';
      return;
    }

    const payload: CreateTenderSource = {
      ...this.sourceForm,
      name: this.sourceForm.name.trim(),
      baseUrl: this.sourceForm.baseUrl.trim(),
      authMode: this.sourceForm.authMode || undefined,
      rateLimitPolicyJson: this.sourceForm.rateLimitPolicyJson || undefined,
      connectorConfigJson: this.sourceForm.connectorConfigJson || undefined,
      isCanary: this.sourceForm.isCanary,
      rolloutStage: this.sourceForm.rolloutStage,
      legalNotes: this.sourceForm.legalNotes || undefined,
      owner: this.sourceForm.owner || undefined
    };

    const request$ = this.editingSourceId
      ? this.apiService.updateTenderSource(this.editingSourceId, payload)
      : this.apiService.createTenderSource(payload);

    request$.subscribe({
      next: () => {
        this.resetSourceForm();
        this.loadSources();
      },
      error: (err) => {
        this.errorSources = err?.message || 'Failed to save source';
      }
    });
  }

  editSource(source: TenderSource): void {
    this.editingSourceId = source.id;
    this.sourceForm = {
      name: source.name,
      type: source.type,
      baseUrl: source.baseUrl,
      authMode: source.authMode,
      pollPriority: source.pollPriority,
      pollIntervalMin: source.pollIntervalMin,
      rateLimitPolicyJson: source.rateLimitPolicyJson,
      connectorConfigJson: source.connectorConfigJson,
      isCanary: source.isCanary,
      rolloutStage: source.rolloutStage,
      isEnabled: source.isEnabled,
      legalNotes: source.legalNotes,
      owner: source.owner
    };
    this.activeTab = 'sources';
  }

  cancelSourceEdit(): void {
    this.resetSourceForm();
  }

  toggleSourceStatus(source: TenderSource): void {
    const payload: CreateTenderSource = {
      name: source.name,
      type: source.type,
      baseUrl: source.baseUrl,
      authMode: source.authMode,
      pollPriority: source.pollPriority,
      pollIntervalMin: source.pollIntervalMin,
      rateLimitPolicyJson: source.rateLimitPolicyJson,
      connectorConfigJson: source.connectorConfigJson,
      isCanary: source.isCanary,
      rolloutStage: source.rolloutStage,
      isEnabled: !source.isEnabled,
      legalNotes: source.legalNotes,
      owner: source.owner
    };

    this.apiService.updateTenderSource(source.id, payload).subscribe({
      next: () => this.loadSources(),
      error: (err) => {
        this.errorSources = err?.message || 'Failed to update source status';
      }
    });
  }

  setRolloutStage(source: TenderSource, stage: 'Canary' | 'Pilot' | 'General' | 'Disabled'): void {
    const payload: UpdateTenderSourceRollout = {
      rolloutStage: stage
    };

    this.apiService.updateTenderSourceRolloutStage(source.id, payload).subscribe({
      next: () => this.loadSources(),
      error: (err) => {
        this.errorSources = err?.message || 'Failed to update rollout stage';
      }
    });
  }

  promoteAll(fromStage: 'Canary' | 'Pilot', toStage: 'Pilot' | 'General'): void {
    this.loadingRollout = true;
    this.errorRollout = '';

    this.apiService.promoteTenderRollout({
      fromStage,
      toStage,
      onlyEnabled: true
    }).subscribe({
      next: () => {
        this.loadingRollout = false;
        this.loadSources();
      },
      error: (err) => {
        this.loadingRollout = false;
        this.errorRollout = err?.message || 'Failed to promote rollout stage';
      }
    });
  }

  deleteSource(id: string): void {
    if (!window.confirm('Delete this source?')) {
      return;
    }

    this.apiService.deleteTenderSource(id).subscribe({
      next: () => this.loadSources(),
      error: (err) => {
        this.errorSources = err?.message || 'Failed to delete source';
      }
    });
  }

  saveRule(): void {
    const payload: CreateTenderNotificationRule = {
      ...this.ruleForm,
      userId: this.ruleForm.userId || undefined,
      countryFilter: this.ruleForm.countryFilter || undefined,
      sectorFilter: this.ruleForm.sectorFilter || undefined,
      authorityFilter: this.ruleForm.authorityFilter || undefined,
      keywords: this.ruleForm.keywords || undefined
    };

    const request$ = this.editingRuleId
      ? this.apiService.updateTenderRule(this.editingRuleId, payload)
      : this.apiService.createTenderRule(payload);

    request$.subscribe({
      next: () => {
        this.resetRuleForm();
        this.loadRules();
      },
      error: (err) => {
        this.errorRules = err?.message || 'Failed to save rule';
      }
    });
  }

  editRule(rule: TenderNotificationRule): void {
    this.editingRuleId = rule.id;
    this.ruleForm = {
      scope: rule.scope,
      channels: rule.channels,
      userId: rule.userId,
      countryFilter: rule.countryFilter,
      sectorFilter: rule.sectorFilter,
      authorityFilter: rule.authorityFilter,
      valueMin: rule.valueMin,
      valueMax: rule.valueMax,
      keywords: rule.keywords,
      isActive: rule.isActive
    };
    this.activeTab = 'rules';
  }

  cancelEdit(): void {
    this.resetRuleForm();
  }

  deleteRule(id: string): void {
    if (!window.confirm('Delete this rule?')) {
      return;
    }

    this.apiService.deleteTenderRule(id).subscribe({
      next: () => this.loadRules(),
      error: (err) => {
        this.errorRules = err?.message || 'Failed to delete rule';
      }
    });
  }

  get filteredRules(): TenderNotificationRule[] {
    const term = this.ruleSearch.trim().toLowerCase();

    return (this.rules || []).filter(rule => {
      if (this.ruleStatusFilter === 'active' && !rule.isActive) return false;
      if (this.ruleStatusFilter === 'inactive' && rule.isActive) return false;

      if (!term) return true;

      const haystack = [
        rule.scope,
        rule.channels,
        rule.countryFilter,
        rule.sectorFilter,
        rule.authorityFilter,
        rule.keywords,
        rule.userId
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();

      return haystack.includes(term);
    });
  }

  private resetRuleForm(): void {
    this.editingRuleId = null;
    this.ruleForm = {
      scope: 'Global',
      channels: 'InApp',
      userId: undefined,
      countryFilter: undefined,
      sectorFilter: undefined,
      authorityFilter: undefined,
      valueMin: undefined,
      valueMax: undefined,
      keywords: undefined,
      isActive: true
    };
  }

  private resetSourceForm(): void {
    this.editingSourceId = null;
    this.sourceForm = {
      name: '',
      type: 'API',
      baseUrl: '',
      authMode: undefined,
      pollPriority: 100,
      pollIntervalMin: 60,
      rateLimitPolicyJson: undefined,
      connectorConfigJson: undefined,
      isCanary: false,
      rolloutStage: 'General',
      isEnabled: true,
      legalNotes: undefined,
      owner: undefined
    };
  }
}
