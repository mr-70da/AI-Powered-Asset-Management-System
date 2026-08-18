import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AiAssistantService } from '../../core/services/ai-assistant.service';
import { AI_EXAMPLES } from '../../core/models/ai-assistant';
import type { Asset, AssetStatus } from '../../core/models/asset';

@Component({
  selector: 'app-ai-assistant',
  imports: [FormsModule, RouterLink],
  templateUrl: './ai-assistant.component.html',
  styleUrl: './ai-assistant.component.scss'
})
export class AiAssistantComponent {
  private readonly aiService = inject(AiAssistantService);

  readonly examples = AI_EXAMPLES;

  readonly question = signal('');
  readonly answer = signal('');
  readonly rows = signal<Asset[]>([]);
  readonly totalCount = signal(0);

  readonly isLoading = signal(false);
  readonly error = signal('');
  readonly asked = signal(false);
  readonly emptyState = signal(false);

  ask(): void {
    const question = this.question().trim();
    if (!question || this.isLoading()) {
      return;
    }

    this.isLoading.set(true);
    this.error.set('');
    this.asked.set(true);

    this.aiService.ask({ question }).subscribe({
      next: (result) => {
        this.answer.set(result.answer);
        this.rows.set(result.rows);
        this.totalCount.set(result.totalCount);
        this.emptyState.set(result.rows.length === 0);
        this.isLoading.set(false);
      },
      error: (err: unknown) => {
        this.isLoading.set(false);
        this.answer.set('');
        this.rows.set([]);
        this.totalCount.set(0);
        this.emptyState.set(false);
        this.error.set(this.describeError(err));
      }
    });
  }

  useExample(example: string): void {
    this.question.set(example);
  }

  statusClass(status: AssetStatus): string {
    return status.toLowerCase().replace(/ /g, '-');
  }

  private describeError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 429) {
        return 'You have sent too many requests. Please wait a moment and try again.';
      }
      if (error.status === 0) {
        return 'Cannot reach the server. Please check your connection.';
      }
      if (error.status >= 500) {
        return 'The assistant is temporarily unavailable. Please try again shortly.';
      }
      return 'Something went wrong while asking the assistant. Please try again.';
    }
    return 'Something went wrong while asking the assistant. Please try again.';
  }
}
