import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  Asset,
  AssetListResponse,
  AssetQuery,
  AssetTransfer,
  CreateAssetRequest,
  TransferAssetRequest,
  UpdateAssetRequest
} from '../models/asset';

@Injectable({ providedIn: 'root' })
export class AssetService {
  constructor(private readonly http: HttpClient) {}

  getAssets(query: AssetQuery): Observable<AssetListResponse> {
    const params = this.toParams(query);
    return this.http.get<AssetListResponse>(`${environment.apiUrl}/api/assets`, { params });
  }

  getAsset(id: number): Observable<Asset> {
    return this.http.get<Asset>(`${environment.apiUrl}/api/assets/${id}`);
  }

  createAsset(request: CreateAssetRequest): Observable<Asset> {
    return this.http.post<Asset>(`${environment.apiUrl}/api/assets`, request);
  }

  updateAsset(id: number, request: UpdateAssetRequest): Observable<Asset> {
    return this.http.put<Asset>(`${environment.apiUrl}/api/assets/${id}`, request);
  }

  retireAsset(id: number): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/api/assets/${id}/retire`, null);
  }

  transferAsset(id: number, request: TransferAssetRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/api/assets/${id}/transfer`, request);
  }

  getTransfers(id: number): Observable<AssetTransfer[]> {
    return this.http.get<AssetTransfer[]>(`${environment.apiUrl}/api/assets/${id}/transfers`);
  }

  private toParams(query: AssetQuery): HttpParams {
    let params = new HttpParams()
      .set('page', query.page.toString())
      .set('pageSize', query.pageSize.toString())
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection);

    params = this.setIfPresent(params, 'search', query.search);
    params = this.setIfPresent(params, 'categoryId', query.categoryId);
    params = this.setIfPresent(params, 'assetTypeId', query.assetTypeId);
    params = this.setIfPresent(params, 'status', query.status);
    params = this.setIfPresent(params, 'departmentId', query.departmentId);
    params = this.setIfPresent(params, 'locationId', query.locationId);
    params = this.setIfPresent(params, 'assignedEmployeeId', query.assignedEmployeeId);
    return params;
  }

  private setIfPresent(params: HttpParams, key: string, value: string | number | null | undefined): HttpParams {
    return value === null || value === undefined || value === ''
      ? params
      : params.set(key, String(value));
  }
}
