import { Output, Injectable, EventEmitter } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { HttpClient } from '@angular/common/http';
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
    let user = await this.authService.getUser();
    return await
      (await firstValueFrom(this.httpClient.get<DogData>(`${environment.apiUrl}/api/dog/${user.data?.id}`))).data;
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
