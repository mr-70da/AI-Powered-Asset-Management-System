import type { UserRole } from './auth';

export type AssetStatus =
  | 'Available'
  | 'Assigned'
  | 'Under Maintenance'
  | 'Retired';

export interface AssetTransfer {
  id: number;
  transferDateUtc: string;
  reason: string | null;
  fromEmployeeName: string | null;
  toEmployeeName: string | null;
  fromDepartmentName: string | null;
  toDepartmentName: string | null;
  fromLocationName: string | null;
  toLocationName: string | null;
  transferredByUserName: string;
}

export interface Asset {
  id: number;
  assetCode: string;
  assetName: string;
  description: string | null;
  categoryId: number;
  categoryName: string;
  assetTypeId: number;
  assetTypeName: string;
  manufacturer: string;
  model: string;
  serialNumber: string | null;
  purchaseDate: string | null;
  purchaseCost: number | null;
  warrantyExpiryDate: string | null;
  status: AssetStatus;
  departmentId: number | null;
  departmentName: string | null;
  assignedEmployeeId: number | null;
  assignedEmployeeName: string | null;
  locationId: number | null;
  locationName: string | null;
  createdByUserId: number | null;
  createdByUserName: string | null;
  createdAtUtc: string;
  modifiedByUserId: number | null;
  modifiedByUserName: string | null;
  modifiedAtUtc: string | null;
  rowVersion: string | null;
  transfers: AssetTransfer[];
}

export interface AssetListResponse {
  items: Asset[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateAssetRequest {
  assetCode: string;
  assetName: string;
  description: string | null;
  categoryId: number;
  assetTypeId: number;
  manufacturer: string;
  model: string;
  serialNumber: string | null;
  purchaseDate: string | null;
  purchaseCost: number | null;
  warrantyExpiryDate: string | null;
  status: AssetStatus;
  departmentId: number | null;
  assignedEmployeeId: number | null;
  locationId: number | null;
}

export interface UpdateAssetRequest extends CreateAssetRequest {}

export interface TransferAssetRequest {
  toDepartmentId: number | null;
  toEmployeeId: number | null;
  toLocationId: number | null;
  transferDate: string;
  reason: string;
  rowVersion: string | null;
}

export interface AssetQuery {
  page: number;
  pageSize: number;
  search: string | null;
  categoryId: number | null;
  assetTypeId: number | null;
  status: AssetStatus | null;
  departmentId: number | null;
  locationId: number | null;
  assignedEmployeeId: number | null;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

export const EMPTY_ASSET_QUERY: AssetQuery = {
  page: 1,
  pageSize: 10,
  search: null,
  categoryId: null,
  assetTypeId: null,
  status: null,
  departmentId: null,
  locationId: null,
  assignedEmployeeId: null,
  sortBy: 'assetCode',
  sortDirection: 'asc'
};

export const ASSET_STATUSES: AssetStatus[] = [
  'Available',
  'Assigned',
  'Under Maintenance',
  'Retired'
];
