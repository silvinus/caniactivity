import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { HttpClient, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http'
import { AppInfoService } from './app-info.service';
import { catchError, firstValueFrom, Observable, Subscriber } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface IUser {
  id: string;
  email: string;
  surname?: string;
  avatarUrl?: string;
}

const defaultPath = '/';

@Injectable()
export class AuthService {
  private _user: IUser | null = null;
  private _displayLogin: boolean = false;
  private _lastAuthenticatedPath: string = defaultPath;
  private $user: Observable<IUser> | undefined;
  private $userSubscriber: Subscriber<IUser> | undefined;

  constructor(private router: Router, private httpClient: HttpClient, private infoService: AppInfoService) {
    this.$user = new Observable((observer) => {
      this.$userSubscriber = observer;
      this.httpClient.post<LoggedUser>(`${environment.apiUrl}/api/accounts/reconnect`, {
        credential: localStorage.getItem("token"),
        provider: parseInt(localStorage.getItem("provider") || "999")
      })
        .pipe(
          catchError(e => {
            if (!this.displayLoginForm) {
              this.logOut();
            }
            throw e;
          })
        ).subscribe(u => observer.next(this.toIUser(u)));
    })
  }

  get loggedIn(): boolean {
    return localStorage.getItem("token") != null; // !!this._user;
  }

  get user(): Observable<IUser> | undefined {
    return this.$user;
  }

  get userEmail(): string | undefined {
    return this._user?.email;
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

  toIUser(loggedUser: LoggedUser) {
    return {
      id: loggedUser.user.id,
      email: loggedUser.user.email,
      avatarUrl: loggedUser.user.avatarUrl,
      surname: (loggedUser.user.firstName.substring(0, 1) + loggedUser.user.lastName.substring(0, 1)).toUpperCase()
    }
  }

  set lastAuthenticatedPath(value: string) {
    this._lastAuthenticatedPath = value;
  }

  async logIn(emailCredential: string, password: string) {
    try {
      let result = await firstValueFrom(this.httpClient.post<LoggedUser>(`${environment.apiUrl}/api/accounts`, {
        email: emailCredential,
        password: password
      }));

      if (!result.isAuthSuccessful) {
        return {
          isOk: false,
          message: "Utilisateur ou mot de passe incorrect"
        };
      }

      this._user = this.toIUser(result);
      this.$userSubscriber?.next(this._user);
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
    let validated = await firstValueFrom(this.httpClient.post<LoggedUser>(`${environment.apiUrl}/api/accounts/validate`, {
      credential: response.credential,
      provider: 1
    }));

    this._user = this.toIUser(validated)
    this.$userSubscriber?.next(this._user);
    this.setToken(validated.token, 1);
    this.router.navigate([this._lastAuthenticatedPath]);
  }

  async getUserInfo() {
    if (this.$user == undefined) return;

    let logged = await firstValueFrom(this.$user)

    return await firstValueFrom(
      this.httpClient.post<any>(`${environment.apiUrl}/api/user/userinfo`, {
        email: logged.email
      })
    );
  }

  async getUser() {
    try {
      if (this.$user == undefined) return { isOk: false, data: null }

      let reconnected = await firstValueFrom(this.$user);

      this._user = reconnected;

      return {
        isOk: true,
        data: this._user
      };
    }
    catch {
      this.logOut();
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
    this.router.navigate([defaultPath]);
  }

  setToken(token: string, provider: number) {
    localStorage.setItem('token', token);
    localStorage.setItem('provider', provider.toString());
  }
}

class User {
  public id: string = "";
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

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // add auth header with jwt if account is logged in and request is to the api url
    //const account = this.accountService.accountValue;
    //const isLoggedIn = account?.token;
    //const isApiUrl = request.url.startsWith(environment.apiUrl);
    if (this.authService.loggedIn) {
      request = request.clone({
        setHeaders: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
    }

    return next.handle(request);
  }
}
