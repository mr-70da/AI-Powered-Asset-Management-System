import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AssetService } from '../../../core/services/asset.service';
import { LookupService } from '../../../core/services/lookup.service';
import type { CanComponentDeactivate } from '../../../core/guards/unsaved-changes.guard';
import { getProblemDetails, type ProblemDetails } from '../../../core/models/problem-details';
import { EMPTY_LOOKUPS, type LookupsResponse } from '../../../core/models/lookup';
import { ASSET_STATUSES, type Asset, type AssetStatus } from '../../../core/models/asset';

@Component({
  selector: 'app-asset-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './asset-form.component.html',
  styleUrl: './asset-form.component.scss'
})
export class AssetFormComponent implements OnInit, CanComponentDeactivate {
  private readonly fb = inject(FormBuilder);
  private readonly assetService = inject(AssetService);
  private readonly lookupService = inject(LookupService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly lookups = signal<LookupsResponse>(EMPTY_LOOKUPS);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly errorMessage = signal('');
  readonly generalErrors = signal<string[]>([]);

  assetId: number | null = null;
  isEdit = false;
  statuses: AssetStatus[] = ASSET_STATUSES;

  readonly form = this.fb.nonNullable.group({
    assetCode: ['', [Validators.required, Validators.maxLength(50)]],
    assetName: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.maxLength(1000)]],
    categoryId: [0 as number, [Validators.required, Validators.min(1)]],
    assetTypeId: [0 as number, [Validators.required, Validators.min(1)]],
    manufacturer: ['', [Validators.maxLength(100)]],
    model: ['', [Validators.maxLength(100)]],
    serialNumber: ['', [Validators.maxLength(100)]],
    purchaseDate: [''],
    purchaseCost: [null as number | null, [Validators.min(0)]],
    warrantyExpiryDate: [''],
    status: ['Available' as AssetStatus, [Validators.required]],
    departmentId: [null as number | null],
    assignedEmployeeId: [null as number | null],
    locationId: [null as number | null]
  });

  ngOnInit(): void {
    this.loadLookups();

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam !== null) {
      const id = Number(idParam);
      if (!Number.isInteger(id) || id <= 0) {
        this.router.navigate(['/not-permitted']);
        return;
      }
      this.assetId = id;
      this.isEdit = true;
      this.loadAsset(id);
    }
  }

  private loadLookups(): void {
    this.lookupService.getLookups().subscribe({
      next: (lookups) => this.lookups.set(lookups)
    });
  }

  private loadAsset(id: number): void {
    this.loading.set(true);
    this.assetService.getAsset(id).subscribe({
      next: (asset) => {
        this.form.patchValue({
          assetCode: asset.assetCode,
          assetName: asset.assetName,
          description: asset.description ?? '',
          categoryId: asset.categoryId,
          assetTypeId: asset.assetTypeId,
          manufacturer: asset.manufacturer,
          model: asset.model,
          serialNumber: asset.serialNumber ?? '',
          purchaseDate: asset.purchaseDate ?? '',
          purchaseCost: asset.purchaseCost,
          warrantyExpiryDate: asset.warrantyExpiryDate ?? '',
          status: asset.status,
          departmentId: asset.departmentId,
          assignedEmployeeId: asset.assignedEmployeeId,
          locationId: asset.locationId
        });
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Failed to load the asset. Please try again.');
      }
    });
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    this.generalErrors.set([]);

    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);

    const request = this.buildRequest();
    const action = this.isEdit
      ? this.assetService.updateAsset(this.assetId!, request)
      : this.assetService.createAsset(request);

    action.subscribe({
      next: (asset) => this.router.navigate(['/assets', asset.id]),
      error: (error: unknown) => this.handleSubmitError(error)
    });
  }

  private buildRequest() {
    const raw = this.form.getRawValue();
    return {
      assetCode: raw.assetCode.trim(),
      assetName: raw.assetName.trim(),
      description: raw.description.trim() || null,
      categoryId: raw.categoryId,
      assetTypeId: raw.assetTypeId,
      manufacturer: raw.manufacturer.trim(),
      model: raw.model.trim(),
      serialNumber: raw.serialNumber.trim() || null,
      purchaseDate: raw.purchaseDate || null,
      purchaseCost: raw.purchaseCost,
      warrantyExpiryDate: raw.warrantyExpiryDate || null,
      status: raw.status,
      departmentId: raw.departmentId,
      assignedEmployeeId: raw.assignedEmployeeId,
      locationId: raw.locationId
    };
  }

  /** Maps a 400 ProblemDetails payload onto the matching form controls (R6.6). */
  private handleSubmitError(error: unknown): void {
    this.submitting.set(false);

    const problem = getProblemDetails(error);
    if (problem) {
      this.mapServerErrors(problem);
      return;
    }

    if (error instanceof HttpErrorResponse) {
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

  /** Returns the first server-side validation message mapped to a control. */
  serverError(controlName: string): string | null {
    const errors = this.form.get(controlName)?.errors?.['server'];
    return Array.isArray(errors) ? (errors[0] ?? null) : (errors ?? null);
  }
}
