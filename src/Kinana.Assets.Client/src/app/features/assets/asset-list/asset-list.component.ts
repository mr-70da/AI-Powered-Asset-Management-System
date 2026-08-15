import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { debounceTime } from 'rxjs';
import { AssetService } from '../../../core/services/asset.service';
import { LookupService } from '../../../core/services/lookup.service';
import { AdminOnlyDirective } from '../../../core/directives/admin-only.directive';
import { EMPTY_LOOKUPS, type LookupsResponse } from '../../../core/models/lookup';
import { ASSET_STATUSES, type Asset, type AssetStatus } from '../../../core/models/asset';

@Component({
  selector: 'app-asset-list',
  imports: [ReactiveFormsModule, RouterLink, AdminOnlyDirective],
  templateUrl: './asset-list.component.html',
  styleUrl: './asset-list.component.scss'
})
export class AssetListComponent implements OnInit {
  private readonly assetService = inject(AssetService);
  private readonly lookupService = inject(LookupService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly assets = signal<Asset[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly lookups = signal<LookupsResponse>(EMPTY_LOOKUPS);

  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly sortBy = signal('assetCode');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');

  readonly statuses: AssetStatus[] = ASSET_STATUSES;

  readonly filterForm = this.fb.group({
    search: [''],
    categoryId: [null as number | null],
    assetTypeId: [null as number | null],
    status: [null as AssetStatus | null],
    departmentId: [null as number | null],
    locationId: [null as number | null],
    assignedEmployeeId: [null as number | null]
  });

  ngOnInit(): void {
    this.loadLookups();
    this.loadAssets();

    this.filterForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef), debounceTime(300))
      .subscribe(() => {
        this.page.set(1);
        this.loadAssets();
      });
  }

  private loadLookups(): void {
    this.lookupService.getLookups().subscribe({
      next: (lookups) => this.lookups.set(lookups),
      error: () => {
        // Filters degrade to empty dropdowns if lookups fail to load.
      }
    });
  }

  loadAssets(): void {
    this.loading.set(true);
    this.error.set('');

    const f = this.filterForm.getRawValue();
    this.assetService
      .getAssets({
        page: this.page(),
        pageSize: this.pageSize(),
        search: f.search || null,
        categoryId: f.categoryId,
        assetTypeId: f.assetTypeId,
        status: f.status,
        departmentId: f.departmentId,
        locationId: f.locationId,
        assignedEmployeeId: f.assignedEmployeeId,
        sortBy: this.sortBy(),
        sortDirection: this.sortDirection()
      })
      .subscribe({
        next: (result) => {
          this.assets.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.error.set(this.describeError(err));
        }
      });
  }

  toggleSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDirection.set('asc');
    }
    this.loadAssets();
  }

  changePage(page: number): void {
    this.page.set(page);
    this.loadAssets();
  }

  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.loadAssets();
  }

  onPageSizeChange(event: Event): void {
    this.changePageSize(Number((event.target as HTMLSelectElement).value));
  }

  totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  }

  pageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.page();
    const start = Math.max(1, Math.min(current - 2, total - 4));
    const end = Math.min(total, start + 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  statusClass(status: AssetStatus): string {
    return status.toLowerCase().replace(/ /g, '-');
  }

  private describeError(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'Cannot reach the server. Please check your connection.';
    }
    return 'Failed to load assets. Please try again.';
  }
}
