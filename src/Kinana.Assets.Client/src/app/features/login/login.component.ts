import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { getProblemDetails } from '../../core/models/problem-details';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    userName: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  submitting = false;
  errorMessage = '';

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.submitting) {
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    const { userName, password } = this.form.getRawValue();

    this.authService.loginAndLoadProfile(userName, password).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        this.router.navigateByUrl(returnUrl && returnUrl.startsWith('/') ? returnUrl : '/assets');
      },
      error: (error: unknown) => {
        this.submitting = false;
        this.errorMessage = this.describeLoginError(error);
      }
    });
  }

  private describeLoginError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) {
        return 'Invalid username or password.';
      }
      const problem = getProblemDetails(error);
      if (problem?.errors) {
        const messages = Object.values(problem.errors).flat();
        return messages.join(' ');
      }
      if (problem?.detail) {
        return problem.detail;
      }
      if (error.status === 0) {
        return 'Cannot reach the server. Is the API running?';
      }
    }
    return 'Something went wrong. Please try again.';
  }
}
