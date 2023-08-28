import { Output, Injectable, EventEmitter } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable()
export class AppointmentService {

  constructor(private httpClient: HttpClient) {
  }

  async validate(appointment: any) {
    return await firstValueFrom(this.httpClient.post<any>(`${environment.apiUrl}/api/appointment/${appointment.id}/validate`,
        appointment));
  }
}

export class Dog {
  id: string = "";
  name: string = "";
  breed: string = "";
}
