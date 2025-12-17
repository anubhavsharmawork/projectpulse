import { Injectable, Injector } from '@angular/core';
import type { MatSnackBar, MatSnackBarRef, TextOnlySnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private snackBar: MatSnackBar | null = null;
  private loadingPromise: Promise<MatSnackBar> | null = null;
  
  constructor(private injector: Injector) {}
  
  /** Lazy load MatSnackBar only when first notification is shown */
  private async getSnackBar(): Promise<MatSnackBar> {
    if (this.snackBar) {
      return this.snackBar;
    }
    
    if (!this.loadingPromise) {
      this.loadingPromise = (async () => {
        // Import the snackbar module and get the service from the root injector
        const { MatSnackBar } = await import('@angular/material/snack-bar');
        // Get MatSnackBar from the injector - it's provided when the module is imported
        this.snackBar = this.injector.get(MatSnackBar);
        return this.snackBar;
      })();
    }
    return this.loadingPromise;
  }
  
  async show(message: string, action: string = 'OK', duration = 3000): Promise<MatSnackBarRef<TextOnlySnackBar> | null> { 
    try {
      const snack = await this.getSnackBar();
      const ref = snack.open(message, action, { 
        duration,
        verticalPosition: 'top',
        horizontalPosition: 'end'
      });
      return ref;
    } catch {
      console.warn('Snackbar not available');
      return null;
    }
  }
  
  async error(message: string, action: string = 'Dismiss', duration = 4000): Promise<MatSnackBarRef<TextOnlySnackBar> | null> { 
    try {
      const snack = await this.getSnackBar();
      const ref = snack.open(message, action, { 
        duration, 
        panelClass: ['snack-error'],
        verticalPosition: 'top',
        horizontalPosition: 'end'
      });
      return ref;
    } catch {
      console.warn('Snackbar not available');
      return null;
    }
  }
}
