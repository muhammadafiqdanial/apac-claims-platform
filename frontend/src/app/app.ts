import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

export interface AuditEntry {
  id: string;
  actor: string;
  note: string;
  status: string;
  timestamp: string;
}

export interface Claim {
  id: string;
  claimNumber: string;
  policyNumber: string;
  claimantName: string;
  market: string;
  type: string;
  status: string;
  description: string;
  estimatedLiability: number;
  settledAmount?: number;
  assignedOfficerId?: string;
  createdAt: string;
  timeline: AuditEntry[];
}

export interface ExposureDashboard {
  totalOutstandingLiability: number;
  totalOpenClaims: number;
  totalSettledAmount: number;
  byMarketAndType: {
    market: string;
    type: string;
    openClaimsCount: number;
    totalOutstandingLiability: number;
    totalSettledAmount: number;
  }[];
  officerWorkload: {
    officerId: string;
    activeClaims: number;
    settledCount: number;
    rejectedCount: number;
  }[];
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5102/api';

  activeRole: 'CLAIMANT' | 'STAFF' = 'CLAIMANT';
  currentOfficerId = 'officer.sarah';
  markets = ['SG', 'MY', 'HK', 'TH', 'AU', 'JP'];

  claims: Claim[] = [];
  selectedClaim: Claim | null = null;
  exposure: ExposureDashboard | null = null;

  filterMarket = '';
  filterStatus = '';

  newClaim = {
    policyNumber: '',
    claimantName: '',
    market: 'SG',
    type: 'Motor',
    description: '',
    estimatedLiability: 5000
  };
  claimantReplyText = '';

  infoQuestion = '';
  settleAmount = 0;
  settleNotes = '';
  rejectReason = '';
  toastMessage = '';

  ngOnInit() {
    this.refreshAll();
  }

  switchRole(role: 'CLAIMANT' | 'STAFF') {
    this.activeRole = role;
    this.selectedClaim = null;
    this.refreshAll();
  }

  refreshAll() {
    const params: string[] = [];
    if (this.filterMarket) params.push(`market=${this.filterMarket}`);
    if (this.filterStatus) params.push(`status=${this.filterStatus}`);
    const query = params.length ? `?${params.join('&')}` : '';

    this.http.get<Claim[]>(`${this.apiUrl}/claims${query}`).subscribe(data => {
      this.claims = data;
      if (this.selectedClaim) {
        this.selectedClaim = data.find(c => c.id === this.selectedClaim!.id) || null;
      }
    });

    this.http.get<ExposureDashboard>(`${this.apiUrl}/dashboard/exposure`).subscribe(data => {
      this.exposure = data;
    });
  }

  selectClaim(claim: Claim) {
    this.selectedClaim = claim;
    this.settleAmount = claim.estimatedLiability;
    this.claimantReplyText = '';
    this.infoQuestion = '';
    this.settleNotes = '';
    this.rejectReason = '';
  }

  submitClaim() {
    if (!this.newClaim.policyNumber || !this.newClaim.claimantName || !this.newClaim.description) return;
    this.http.post<Claim>(`${this.apiUrl}/claims`, this.newClaim).subscribe({
      next: (created) => {
        this.showToast(`Claim ${created.claimNumber} submitted!`);
        this.newClaim = { policyNumber: '', claimantName: '', market: 'SG', type: 'Motor', description: '', estimatedLiability: 5000 };
        setTimeout(() => this.refreshAll(), 300);
        this.selectClaim(created);
      },
      error: (err) => this.showToast(err.error?.error || 'Submission failed')
    });
  }

  provideInfo() {
    if (!this.selectedClaim || !this.claimantReplyText.trim()) return;
    this.http.post<Claim>(`${this.apiUrl}/claims/${this.selectedClaim.id}/provide-info`, {
      claimantName: this.selectedClaim.claimantName,
      response: this.claimantReplyText
    }).subscribe({
      next: () => {
        this.showToast('Additional information sent to claims officer.');
        this.claimantReplyText = '';
        setTimeout(() => this.refreshAll(), 300);
      },
      error: (err) => this.showToast(err.error?.error || 'Action failed')
    });
  }

  assignToMe() {
    if (!this.selectedClaim) return;
    this.http.post<Claim>(`${this.apiUrl}/claims/${this.selectedClaim.id}/assign`, {
      officerId: this.currentOfficerId
    }).subscribe({
      next: () => {
        this.showToast(`Claim assigned to ${this.currentOfficerId}`);
        setTimeout(() => this.refreshAll(), 300);
      }
    });
  }

  requestInfo() {
    if (!this.selectedClaim || !this.infoQuestion.trim()) return;
    this.http.post<Claim>(`${this.apiUrl}/claims/${this.selectedClaim.id}/request-info`, {
      officerId: this.currentOfficerId,
      question: this.infoQuestion
    }).subscribe({
      next: () => {
        this.showToast('Requested additional info from claimant.');
        this.infoQuestion = '';
        setTimeout(() => this.refreshAll(), 300);
      }
    });
  }

  settleClaim() {
    if (!this.selectedClaim) return;
    this.http.post<Claim>(`${this.apiUrl}/claims/${this.selectedClaim.id}/settle`, {
      officerId: this.currentOfficerId,
      settledAmount: Number(this.settleAmount),
      notes: this.settleNotes || 'Approved for settlement'
    }).subscribe({
      next: () => {
        this.showToast('Claim settled & liability exposure updated.');
        setTimeout(() => this.refreshAll(), 300);
      }
    });
  }

  rejectClaim() {
    if (!this.selectedClaim || !this.rejectReason.trim()) return;
    this.http.post<Claim>(`${this.apiUrl}/claims/${this.selectedClaim.id}/reject`, {
      officerId: this.currentOfficerId,
      reason: this.rejectReason
    }).subscribe({
      next: () => {
        this.showToast('Claim rejected.');
        setTimeout(() => this.refreshAll(), 300);
      }
    });
  }

  private showToast(msg: string) {
    this.toastMessage = msg;
    setTimeout(() => (this.toastMessage = ''), 4000);
  }
}