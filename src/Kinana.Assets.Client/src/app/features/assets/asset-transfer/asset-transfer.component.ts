import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AssetService } from '../../../core/services/asset.service';
import { LookupService } from '../../../core/services/lookup.service';
import type { CanComponentDeactivate } from '../../../core/guards/unsaved-changes.guard';
import { getProblemDetails, type ProblemDetails } from '../../../core/models/problem-details';
import { EMPTY_LOOKUPS, type LookupsResponse } from '../../../core/models/lookup';
import type { Asset } from '../../../core/models/asset';

@Component({
  selector: 'app-asset-transfer',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './asset-transfer.component.html',
  styleUrl: './asset-transfer.component.scss'
})
export class AssetTransferComponent implements OnInit, CanComponentDeactivate {
  private readonly fb = inject(FormBuilder);
  private readonly assetService = inject(AssetService);
  private readonly lookupService = inject(LookupService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly asset = signal<Asset | null>(null);
  readonly lookups = signal<LookupsResponse>(EMPTY_LOOKUPS);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly errorMessage = signal('');
  readonly generalErrors = signal<string[]>([]);

  readonly form = this.fb.nonNullable.group({
    toDepartmentId: [null as number | null],
    toEmployeeId: [null as number | null],
    toLocationId: [null as number | null],
    transferDate: [this.today(), [Validators.required, this.notInFuture]],
    reason: ['', [Validators.required, Validators.maxLength(500)]]
  });

  private assetId = 0;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id <= 0) {
      this.router.navigate(['/not-permitted']);
      return;
    }
    this.assetId = id;
    this.loadAsset();
    this.loadLookups();
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private notInFuture = (control: { value: string }): { futureDate: true } | null => {
    if (!control.value) {
      return null;
    }
    return control.value > new Date().toISOString().slice(0, 10) ? { futureDate: true } : null;
  };

  private loadAsset(): void {
    this.assetService.getAsset(this.assetId).subscribe({
      next: (asset) => {
        this.asset.set(asset);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Failed to load the asset. Please try again.');
      }
    });
  }

  private loadLookups(): void {
    this.lookupService.getLookups().subscribe({
      next: (lookups) => this.lookups.set(lookups)
    });
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    this.generalErrors.set([]);

    if (this.form.invalid || this.submitting()) {
      return;
    }

    const asset = this.asset();
    if (!asset) {
      return;
    }

    this.submitting.set(true);
    const raw = this.form.getRawValue();

    this.assetService
      .transferAsset(this.assetId, {
        toDepartmentId: raw.toDepartmentId,
        toEmployeeId: raw.toEmployeeId,
        toLocationId: raw.toLocationId,
        transferDate: raw.transferDate,
        reason: raw.reason.trim(),
        rowVersion: asset.rowVersion
      })
      .subscribe({
        next: () => this.router.navigate(['/assets', this.assetId]),
        error: (error: unknown) => this.handleSubmitError(error)
      });
  }

  private handleSubmitError(error: unknown): void {
    this.submitting.set(false);

    if (error instanceof HttpErrorResponse) {
      if (error.status === 409) {
        this.generalErrors.set([
          'Concurrency conflict: this asset was modified by someone else. Re-open it to load the latest version and try again.'
        ]);
        return;
      }
      if (error.status === 400) {
        const problem = getProblemDetails(error);
        if (problem) {
          this.mapServerErrors(problem);
          return;
        }
      }
      if (error.status === 401 || error.status === 403) {
        return; // Handled centrally by the HTTP interceptor.
      }
      if (error.status === 0) {
        this.generalErrors.set(['Cannot reach the server. Please check your connection.']);
        return;
      }
    }

    this.generalErrors.set(['Something went wrong. Please try again.']);
  }

  private mapServerErrors(problem: ProblemDetails): void {
    const fieldErrors: string[] = [];
    if (problem.errors) {
      for (const [property, messages] of Object.entries(problem.errors)) {
        const control = this.form.get(property);
        if (control) {
          control.setErrors({ server: messages });
          control.markAsTouched();
        } else {
          fieldErrors.push(...messages);
        }
      }
    }

    this.generalErrors.set([
      ...fieldErrors,
      ...(problem.detail ? [problem.detail] : [])
    ]);
  }

  /** R6.4: warn before leaving the form with unsaved changes. */
  canDeactivate(): boolean {
    if (this.form.dirty && !this.submitting()) {
      return window.confirm(
        'You have unsaved changes. Are you sure you want to leave this page?'
      );
    }
    return true;
  }

  serverError(controlName: string): string | null {
    const errors = this.form.get(controlName)?.errors?.['server'];
    return Array.isArray(errors) ? (errors[0] ?? null) : (errors ?? null);
  }
}
