import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { HttpClient, HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http'
import { AppInfoService } from './app-info.service';
import { BehaviorSubject, catchError, filter, firstValueFrom, map, Observable, of, Subscriber, switchMap, take, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Token } from '@angular/compiler';

export interface IUser {
  id: string;
  email: string;
  surname?: string;
  avatarUrl?: string;
}

const defaultPath = '/';

@Injectable()
export class AuthService {
  private _user: IUser = { id: "", email: ""};
  private _displayLogin: boolean = false;
  private _lastAuthenticatedPath: string = defaultPath;
  private $user: Observable<IUser> | undefined;
  private $userSubscriber: Subscriber<IUser> | undefined;
  private ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

  constructor(private router: Router, private httpClient: HttpClient, private infoService: AppInfoService) {
    this.$user = new Observable((observer) => {
      this.$userSubscriber = observer;
      this.httpClient.post<LoggedUser>(`${environment.apiUrl}/api/accounts/reconnect`, {
        credential: localStorage.getItem("token"),
        provider: parseInt(localStorage.getItem("provider") || "999")
      })
        /*.pipe(
          catchError(e => {
            if (!this.displayLoginForm) {
              this.logOut();
            }
            throw e;
          })
        )*/.subscribe(u => observer.next(this.toIUser(u)));
    })
  }

  get loggedIn(): boolean {
    return localStorage.getItem("token") != null; // !!this._user;
  }

  get user(): Observable<IUser> | undefined {
    return this.$user;
  }

  get roles(): string {
    let token = localStorage.getItem('token') || undefined;
    if (token == undefined) return '';

    let decoded = (JSON.parse(atob(token.split('.')[1])));
    return decoded[this.ROLE_CLAIM];
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
      this.setToken(result.token, result.refreshToken, 0);
      this.setUser(this._user);
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
    this.setToken(validated.token, validated.refreshToken, 1);
    this.setUser(this._user);
    this.router.navigate([this._lastAuthenticatedPath]);
  }

  async getUserInfo() {
    if (this.$user == undefined) return;

    let logged = this.getUser();

    return await firstValueFrom(
      this.httpClient.post<any>(`${environment.apiUrl}/api/user/userinfo`, {
        email: logged.data.email
      })
    );
  }

  getUser() {
    try {
      let serialized = localStorage.getItem("user");
      if (serialized == undefined) return { isOk: false, data: { id: "", email: "" } }

      this._user = JSON.parse(serialized);

      return {
        isOk: true,
        data: this._user
      };
    }
    catch {
      this.logOut();
      return {
        isOk: false,
        data: { id: "", email: "" }
      };
    }
  }

  async createAccount(email: string, password: string, confirmPassword: string, firstname: string, lastname: string) {
    try {
      await firstValueFrom(this.httpClient.post<LoggedUser>(`${environment.apiUrl}/api/accounts/registration`, {
        email: email,
        password: password,
        confirmPassword: confirmPassword,
        firstName: firstname,
        lastName: lastname
      }));

      this.router.navigate(['/create-account']);
      return {
        isOk: true
      };
    }
    catch (e: any) {
      return {
        isOk: false,
        message: `Impossible de créer le compte: ${Object.keys(e.error.errors).map(r => e.error.errors[r]).join('<br>')}`
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
    this._user = { id: "", email: "" };
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('provider');
    localStorage.removeItem('user');
    this.$userSubscriber?.next(undefined);
    // TODO revoke
    this.router.navigate([defaultPath]);
  }

  tryRefreshingTokens(token: string | null): Observable<boolean> {
    const refreshToken: string | null = localStorage.getItem("refreshToken");
    if (!token || !refreshToken) {
      return of(false);
    }

    const credentials = JSON.stringify({ accessToken: token, refreshToken: refreshToken });

    return this.httpClient.post<RefreshedToken>(`${environment.apiUrl}/api/accounts/refresh`, credentials)
      .pipe(
        map(tokens => {
          this.setToken(tokens.accessToken, tokens.refreshToken, Number.parseInt(localStorage.getItem("Provider") || "999"));
          return true;
        }),
        catchError((e: any) => {
          console.error(e);
          this.logOut();
          throw 'error when refresh token. Details: ' + e;
        })
      )
      //.subscribe({
      //  next: tokens => {
      //    this.setToken(tokens.accessToken, tokens.refreshToken, Number.parseInt(localStorage.getItem("Provider") || "999"));
      //  },
      //  error: (e) => {
      //    console.error(e);
      //    this.logOut();
      //  }
      //})

    //try {
    //  let tokens = await firstValueFrom(
    //    this.httpClient.post<RefreshedToken>(`${environment.apiUrl}/api/accounts/refresh`, credentials)
    //  );
    //  this.setToken(tokens.accessToken, tokens.refreshToken, Number.parseInt(localStorage.getItem("Provider") || "999"));
    //  return true;
    //}
    //catch (e) {
    //  console.error(e);
    //  this.logOut();
    //  return false;
    //}
  }

  setToken(token: string, refershToken: string, provider: number) {
    localStorage.setItem('token', token);
    localStorage.setItem('refreshToken', refershToken);
    localStorage.setItem('provider', provider.toString());
  }

  setUser(user: IUser) {
    localStorage.setItem('user', JSON.stringify(user));
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
  public refreshToken: string = "";
}

class RefreshedToken {
  public accessToken: string = "";
  public refreshToken: string = "";
}

@Injectable()
export class AuthGuardService implements CanActivate {
  constructor(private router: Router, private authService: AuthService) { }

  private isTokenExpired(token: string) {
    const expiry = (JSON.parse(atob(token.split('.')[1]))).exp;
    return (Math.floor((new Date).getTime() / 1000)) >= expiry;
  }

  async canActivate(route: ActivatedRouteSnapshot): Promise<boolean> {
    const isLoggedIn = this.authService.loggedIn;
    const isAuthForm = [
      'login-form',
      'reset-password',
      'create-account',
      'change-password/:recoveryCode'
    ].includes(route.routeConfig?.path || defaultPath);
    const isPublicPage = [
      'home', 'pages/activities', 'pages/environment', 'pages/tarifs'
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

    let token = localStorage.getItem("token");
    if (token && this.isTokenExpired(token)) {
      const isRefreshSuccess = await firstValueFrom(this.authService.tryRefreshingTokens(token));
      if (!isRefreshSuccess && !isPublicPage) {
        return false;
      }
    }

    return isLoggedIn || isAuthForm || isPublicPage;
  }
}

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

  constructor(private authService: AuthService) { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    let authReq = request;

    if (this.authService.loggedIn) {
      authReq = this.addHeaders(request, localStorage.getItem('token') || "");
    }

    return next.handle(authReq).pipe(catchError(error => {
      if (error instanceof HttpErrorResponse /*&& !authReq.url.includes('auth/signin')*/ && error.status === 401) {
        return this.handle401Error(authReq, next);
      }

      return throwError(() => error);
    }));
  }

  private handle401Error(request: HttpRequest<any>, next: HttpHandler) {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      return this.authService.tryRefreshingTokens(localStorage.getItem('token') || "")
        .pipe(
          switchMap((isRefreshed: boolean) => {
            this.isRefreshing = false;
            if (isRefreshed) {
              this.refreshTokenSubject.next(localStorage.getItem('token'));
              return next.handle(this.addHeaders(request, localStorage.getItem('token') || ""));
            }
            return throwError(() => new Error("Token not refreshed"));
          }),
          catchError((e) => {
            this.isRefreshing = false;
            return throwError(() => e);
          })
        );
    }

    return this.refreshTokenSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap((token) => next.handle(this.addHeaders(request, token)))
    );
  }

    private addHeaders(request: HttpRequest<any>, token: string) {
    let contentType = {
      "Content-type": request.headers.get("Content-type") || "application/json"
    };
    return request.clone({
      setHeaders: {
        ...contentType,
        Authorization: `Bearer ${localStorage.getItem('token')}`
      }
    })
  }
}
