import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { HttpClient } from '@angular/common/http'
import { AppInfoService } from './app-info.service';
import { firstValueFrom } from 'rxjs';

export interface IUser {
  email: string;
  surname?: string;
  avatarUrl?: string;
}

const defaultPath = '/';

@Injectable()
export class AuthService {
  private _user: IUser | null = null;
  private _displayLogin: boolean = false;
  get loggedIn(): boolean {
    return !!this._user;
  }

  get displayLoginForm(): boolean {
    const isAuthForm = [
      '/login-form',
      '/reset-password',
      '/create-account',
      '/change-password/:recoveryCode'
    ].includes(this.router.url)

    return isAuthForm;
  }

  private _lastAuthenticatedPath: string = defaultPath;

  set lastAuthenticatedPath(value: string) {
    this._lastAuthenticatedPath = value;
  }

  constructor(private router: Router, private httpClient: HttpClient, private infoService: AppInfoService) { }

  async logIn(emailCredential: string, password: string) {

    try {
      let result = await firstValueFrom(this.httpClient.post<LoggedUser>(`${this.infoService.backendUrl}/api/accounts`, {
        email: emailCredential,
        password: password
      }));

      if (!result.isAuthSuccessful) {
        return {
          isOk: false,
          message: "Utilisateur ou mot de passe incorrect"
        };
      }

      this._user = {
        email: result.user.email,
        avatarUrl: result.user.avatarUrl,
        surname: (result.user.firstName.substring(0, 1) + result.user.lastName.substring(0, 1)).toUpperCase()
      }
      this.setToken(result.token, 0);
      this.router.navigate([this._lastAuthenticatedPath]);

      return {
        isOk: true,
        data: this._user
      };
    }
    catch (e) {
      return {
        isOk: false,
        message: "Authentication failed"
      };
    }
  }

  async logInWithGoogle(response: any) {
    let validated = await firstValueFrom(this.httpClient.post<LoggedUser>(`${this.infoService.backendUrl}/api/accounts/validate`, {
      credential: response.credential,
      provider: 1
    }));

    this._user = {
      email: validated.user.email,
      avatarUrl: validated.user.avatarUrl,
      surname: (validated.user.firstName.substring(0, 1) + validated.user.lastName.substring(0, 1)).toUpperCase()
    }
    this.setToken(validated.token, 1);
    this.router.navigate([this._lastAuthenticatedPath]);
  }

  async getUser() {
    try {
      if (localStorage.getItem("token") == null) return { isOk: false, data: null };


      let reconnected = await firstValueFrom(this.httpClient.post<LoggedUser>(`${this.infoService.backendUrl}/api/accounts/reconnect`, {
        credential: localStorage.getItem("token"),
        provider: parseInt(localStorage.getItem("provider") || "999")
      }));

      this._user = {
        email: reconnected.user.email,
        avatarUrl: reconnected.user.avatarUrl,
        surname: (reconnected.user.firstName.substring(0, 1) + reconnected.user.lastName.substring(0, 1)).toUpperCase()
      }

      return {
        isOk: true,
        data: this._user
      };
    }
    catch {
      return {
        isOk: false,
        data: null
      };
    }
  }

  async createAccount(email: string, password: string) {
    try {
      // Send request

      this.router.navigate(['/create-account']);
      return {
        isOk: true
      };
    }
    catch {
      return {
        isOk: false,
        message: "Failed to create account"
      };
    }
  }

  async changePassword(email: string, recoveryCode: string) {
    try {
      // Send request

      return {
        isOk: true
      };
    }
    catch {
      return {
        isOk: false,
        message: "Failed to change password"
      }
    }
  }

  async resetPassword(email: string) {
    try {
      // Send request

      return {
        isOk: true
      };
    }
    catch {
      return {
        isOk: false,
        message: "Failed to reset password"
      };
    }
  }

  async logOut() {
    this._user = null;
    localStorage.removeItem('token');
    localStorage.removeItem('provider');
  }

  setToken(token: string, provider: number) {
    localStorage.setItem('token', token);
    localStorage.setItem('provider', provider.toString());
  }
}

class User {
  public email: string = "";
  public firstName: string = "";
  public lastName: string = "";
  public avatarUrl: string | undefined;
}

class LoggedUser {
  public isAuthSuccessful: boolean = false;
  public user: User = new User();
  public token: string = "";
}

@Injectable()
export class AuthGuardService implements CanActivate {
  constructor(private router: Router, private authService: AuthService) { }

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const isLoggedIn = this.authService.loggedIn;
    const isAuthForm = [
      'login-form',
      'reset-password',
      'create-account',
      'change-password/:recoveryCode'
    ].includes(route.routeConfig?.path || defaultPath);
    const isPublicPage = [
      'home', 'pages/activities', 'pages/environment'
    ].includes(route.routeConfig?.path || defaultPath);

    if (isLoggedIn && isAuthForm) {
      this.authService.lastAuthenticatedPath = defaultPath;
      this.router.navigate([defaultPath]);
      return false;
    }

    if (!isLoggedIn && !isAuthForm && !isPublicPage) {
      this.router.navigate(['/login-form']);
    }

    if (isLoggedIn) {
      this.authService.lastAuthenticatedPath = route.routeConfig?.path || defaultPath;
    }

    return isLoggedIn || isAuthForm || isPublicPage;
  }
}
