import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { AiChatRequest, AiChatResponse } from '../models/ai-assistant';

/**
 * Typed access to the AI assistant. All AI traffic goes through the API — the
 * client never holds a provider key or calls the provider directly (R4.6).
 */
@Injectable({ providedIn: 'root' })
export class AiAssistantService {
  constructor(private readonly http: HttpClient) {}

  ask(request: AiChatRequest): Observable<AiChatResponse> {
    return this.http.post<AiChatResponse>(`${environment.apiUrl}/api/ai/ask`, request);
  }
}
