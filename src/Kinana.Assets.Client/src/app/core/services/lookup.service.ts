import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { LookupsResponse } from '../models/lookup';

@Injectable({ providedIn: 'root' })
export class LookupService {
  constructor(private readonly http: HttpClient) {}

  getLookups(): Observable<LookupsResponse> {
    return this.http.get<LookupsResponse>(`${environment.apiUrl}/api/lookups`);
  }
}
