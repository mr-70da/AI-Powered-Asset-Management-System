import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AssetService } from '../../../core/services/asset.service';
import { AuthService } from '../../../core/services/auth.service';
import { AdminOnlyDirective } from '../../../core/directives/admin-only.directive';
import type { Asset } from '../../../core/models/asset';

@Component({
  selector: 'app-asset-detail',
  imports: [RouterLink, DatePipe, DecimalPipe, AdminOnlyDirective],
  templateUrl: './asset-detail.component.html',
  styleUrl: './asset-detail.component.scss'
})
export class AssetDetailComponent implements OnInit {
  private readonly assetService = inject(AssetService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  readonly asset = signal<Asset | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly retiring = signal(false);

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id <= 0) {
      this.router.navigate(['/not-permitted']);
      return;
    }
    this.loadAsset(id);
  }

  private loadAsset(id: number): void {
    this.loading.set(true);
    this.error.set('');
    this.assetService.getAsset(id).subscribe({
      next: (asset) => {
        this.asset.set(asset);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.loading.set(false);
        this.error.set(this.describeError(err));
      }
    });
  }

  retire(): void {
    const asset = this.asset();
    if (!asset) {
      return;
    }
    if (!window.confirm(`Retire asset "${asset.assetCode}"? This cannot be undone.`)) {
      return;
    }
    this.retiring.set(true);
    this.assetService.retireAsset(asset.id).subscribe({
      next: () => {
        this.retiring.set(false);
        this.loadAsset(asset.id);
      },
      error: () => {
        this.retiring.set(false);
        this.error.set('Failed to retire the asset. Please try again.');
      }
    });
  }

  statusClass(status: string): string {
    return status.toLowerCase().replace(/ /g, '-');
  }

  private describeError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 404) {
        return 'This asset could not be found. It may have been removed.';
      }
      if (error.status === 0) {
        return 'Cannot reach the server. Please check your connection.';
      }
    }
    return 'Failed to load the asset. Please try again.';
  }
}
