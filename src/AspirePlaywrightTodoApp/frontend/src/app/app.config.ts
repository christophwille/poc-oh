import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  authInterceptor,
  provideAuth,
  StsConfigHttpLoader,
  StsConfigLoader,
} from 'angular-auth-oidc-client';
import { map } from 'rxjs';

interface AppConfigResponse {
  authority: string;
  clientId: string;
  scope: string;
}

// OIDC settings come from the backend at runtime because the Aspire AppHost
// assigns service URLs dynamically (random ports in test runs).
const authConfigLoaderFactory = (http: HttpClient) => {
  const config$ = http.get<AppConfigResponse>('/api/config').pipe(
    map((cfg) => ({
      authority: cfg.authority,
      clientId: cfg.clientId,
      scope: cfg.scope,
      redirectUrl: window.location.origin,
      postLogoutRedirectUri: window.location.origin,
      responseType: 'code',
      silentRenew: false,
      secureRoutes: ['/api/todos'],
    })),
  );
  return new StsConfigHttpLoader(config$);
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([authInterceptor()])),
    provideAuth({
      loader: {
        provide: StsConfigLoader,
        useFactory: authConfigLoaderFactory,
        deps: [HttpClient],
      },
    }),
  ],
};
