import { Output, Injectable, EventEmitter } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable()
export class UserService {
  @Output() changed = new EventEmitter();

  constructor(private httpClient: HttpClient,
    private authService: AuthService) {
  }

  async getDogs() {
    let user = this.authService.getUser();
    return await
      (await firstValueFrom(this.httpClient.get<DogData>(`${environment.apiUrl}/api/dog/${user.data?.id}`))).data;
  }

  async getValidateDogs() {
    let user = this.authService.getUser();
    let filter = '["status", "=", 1]'
    return await
      (await firstValueFrom(this.httpClient.get<DogData>(`${environment.apiUrl}/api/dog/${user.data?.id}?filter=${filter}`))).data;
  }

  async updateUser(key: string, user: User) {
    const params = new HttpParams({
      fromObject: {
        key: key,
        values: JSON.stringify(user),
      }
    })
    return (await firstValueFrom(this.httpClient.put<any>(`${environment.apiUrl}/api/user`, params, { headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' }) })));
  }

  async deleteUser(key: string) {
    const params = new HttpParams({
      fromObject: {
        key: key
      }
    });
    const options = {
      headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' }),
      body: params
    };

    return (await firstValueFrom(this.httpClient.delete<any>(`${environment.apiUrl}/api/user`, options)));
  }
}

export class DogData {
  data: Array<Dog> = [];
  breed: string = "";
}

export class Dog {
  name: string = "";
  breed: string = "";
}

export class User { }
