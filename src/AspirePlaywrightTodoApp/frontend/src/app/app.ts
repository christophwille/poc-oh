import { Component, OnInit, inject, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { TodoList } from './todo-list';

@Component({
  selector: 'app-root',
  imports: [TodoList],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);

  protected readonly authenticated = signal(false);
  protected readonly userName = signal('');
  protected readonly checking = signal(true);

  ngOnInit(): void {
    // Processes the authorization-code callback when redirected back from the auth server.
    this.oidcSecurityService.checkAuth().subscribe(({ isAuthenticated, userData }) => {
      this.authenticated.set(isAuthenticated);
      this.userName.set(userData?.name ?? userData?.sub ?? '');
      this.checking.set(false);
    });
  }

  protected login(): void {
    this.oidcSecurityService.authorize();
  }

  protected logout(): void {
    this.oidcSecurityService.logoff().subscribe();
  }
}
