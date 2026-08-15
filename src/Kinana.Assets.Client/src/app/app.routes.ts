import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { unsavedChangesGuard } from './core/guards/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'not-permitted',
    loadComponent: () =>
      import('./features/not-permitted/not-permitted.component').then((m) => m.NotPermittedComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent)
      },
      {
        path: 'assets',
        loadComponent: () =>
          import('./features/assets/asset-list/asset-list.component').then((m) => m.AssetListComponent)
      },
      {
        path: 'ai-assistant',
        loadComponent: () =>
          import('./features/ai-assistant/ai-assistant.component').then((m) => m.AiAssistantComponent)
      },
      {
        // Admin-only features are lazy-loaded and gated with canMatch so a
        // standard User never downloads these bundles (R6.2).
        path: 'assets/new',
        canMatch: [adminGuard],
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./features/assets/asset-form/asset-form.component').then((m) => m.AssetFormComponent)
      },
      {
        path: 'assets/:id',
        loadComponent: () =>
          import('./features/assets/asset-detail/asset-detail.component').then((m) => m.AssetDetailComponent)
      },
      {
        path: 'assets/:id/edit',
        canMatch: [adminGuard],
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./features/assets/asset-form/asset-form.component').then((m) => m.AssetFormComponent)
      },
      {
        path: 'assets/:id/transfer',
        canMatch: [adminGuard],
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./features/assets/asset-transfer/asset-transfer.component').then((m) => m.AssetTransferComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'assets' }
];
