import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent
} from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * Ensures all outbound request bodies containing date strings or Date objects
 * are normalized to ISO 8601 UTC format (yyyy-MM-ddTHH:mm:ss.fffZ).
 *
 * Date-only strings (yyyy-MM-dd) from HTML date inputs are preserved as-is
 * because the backend converter handles them correctly.
 */
@Injectable()
export class Iso8601Interceptor implements HttpInterceptor {
  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (req.body && typeof req.body === 'object' && !(req.body instanceof FormData)) {
      const normalized = this.normalizeDates(req.body);
      return next.handle(req.clone({ body: normalized }));
    }
    return next.handle(req);
  }

  private normalizeDates(obj: unknown): unknown {
    if (obj === null || obj === undefined) {
      return obj;
    }

    if (obj instanceof Date) {
      return obj.toISOString();
    }

    if (Array.isArray(obj)) {
      return obj.map(item => this.normalizeDates(item));
    }

    if (typeof obj === 'object') {
      const result: Record<string, unknown> = {};
      for (const [key, value] of Object.entries(obj as Record<string, unknown>)) {
        if (value instanceof Date) {
          result[key] = value.toISOString();
        } else if (typeof value === 'object' && value !== null && !(value instanceof FormData)) {
          result[key] = this.normalizeDates(value);
        } else {
          result[key] = value;
        }
      }
      return result;
    }

    return obj;
  }
}
