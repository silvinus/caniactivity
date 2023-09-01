import { Component, OnInit } from '@angular/core';
import { environment } from '../../../environments/environment';
import * as AspNetData from 'devextreme-aspnet-data-nojquery';
import { AppInfoService } from '../../shared/services';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})
export class UsersComponent implements OnInit {
  dataSource: any;
  providers: any;
  statuses: any;

  constructor(private infoService: AppInfoService) {
    this.providers = infoService.providers;
    this.statuses = infoService.statuses;
  }

  ngOnInit(): void {
    this.dataSource = AspNetData.createStore({
      key: 'id',
      loadUrl: `${environment.apiUrl}/api/user`,
      insertUrl: `${environment.apiUrl}/api/user`,
      updateUrl: `${environment.apiUrl}/api/user`,
      deleteUrl: `${environment.apiUrl}/api/user`,
      onBeforeSend(method, ajaxOptions) {
        ajaxOptions.headers = {
          "Authorization": `Bearer ${localStorage.getItem('token')}`
        };
      },
    });
  }

}
