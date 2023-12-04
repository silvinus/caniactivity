import { Output, Injectable, EventEmitter } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable()
export class FileService {
  @Output() changed = new EventEmitter();

  constructor(private httpClient: HttpClient) {
  }

  async getFiles() {
    let result = (await firstValueFrom(this.httpClient.get<FilesResponse>(`${environment.apiUrl}/api/files?command=GetDirContents&arguments=all`)));
    return result.result;
  }
}

export class FilesResponse {
  success: boolean = false;
  errorCode: string = "";
  errorText: string = "";
  result: Array<File> = []
}

export class File {
  key: string = "";
  name: string = "";
  dateModified: string = "";
  isDirectory: boolean = false;
  size: number = 0;
  widthRatio: number = 1;
  heightRatio: number = 1;
}
