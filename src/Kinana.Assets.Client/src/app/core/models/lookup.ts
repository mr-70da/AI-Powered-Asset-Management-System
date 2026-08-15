export interface LookupItem {
  id: number;
  name: string;
}

export interface LookupsResponse {
  categories: LookupItem[];
  assetTypes: LookupItem[];
  departments: LookupItem[];
  locations: LookupItem[];
  employees: LookupItem[];
}

export const EMPTY_LOOKUPS: LookupsResponse = {
  categories: [],
  assetTypes: [],
  departments: [],
  locations: [],
  employees: []
};
