import { CanDeactivateFn } from '@angular/router';

export interface CanComponentDeactivate {
  canDeactivate(): boolean;
}

/**
 * Warns before navigating away from a form with unsaved changes (R6.4).
 * Components opt in by implementing `canDeactivate()` (e.g. checking a
 * `form.dirty` flag and confirming with the user).
 */
export const unsavedChangesGuard: CanDeactivateFn<CanComponentDeactivate> = (component) => {
  return component?.canDeactivate() ?? true;
};
