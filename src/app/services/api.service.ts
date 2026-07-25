import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

interface CreditPlusRuntimeConfig {
  apiBaseUrl?: string;
}

declare global {
  interface Window {
    CREDIT_PLUS_CONFIG?: CreditPlusRuntimeConfig;
  }
}

function apiBaseUrl(): string {
  const configured = window.CREDIT_PLUS_CONFIG?.apiBaseUrl?.trim() ?? '';
  return configured.replace(/\/+$/, '');
}

export function apiUrl(path: string): string {
  const cleanPath = path.startsWith('/') ? path : `/${path}`;
  return `${apiBaseUrl()}${cleanPath}`;
}

export function assetUrl(path: string): string {
  if (!path || path.startsWith('http') || path.startsWith('/')) return path;
  return path.startsWith('uploads/') ? apiUrl(`/${path}`) : `/${path}`;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  get<T>(path: string) {
    return this.http.get<T>(apiUrl(path), { withCredentials: true });
  }

  post<T>(path: string, body: unknown) {
    return this.http.post<T>(apiUrl(path), body, { withCredentials: true });
  }

  put<T>(path: string, body: unknown) {
    return this.http.put<T>(apiUrl(path), body, { withCredentials: true });
  }

  delete<T>(path: string) {
    return this.http.delete<T>(apiUrl(path), { withCredentials: true });
  }
}
