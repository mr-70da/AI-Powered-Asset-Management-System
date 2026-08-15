import { HttpErrorResponse } from '@angular/common/http';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export function getProblemDetails(error: unknown): ProblemDetails | null {
  if (error instanceof HttpErrorResponse && error.status === 400) {
    const body = error.error;
    if (body && typeof body === 'object') {
      return body as ProblemDetails;
    }
  }
  return null;
}
