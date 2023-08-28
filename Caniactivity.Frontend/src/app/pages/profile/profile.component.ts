import { Component, OnInit } from '@angular/core';
import { AuthService, IUser } from '../../shared/services';
import * as AspNetData from 'devextreme-aspnet-data-nojquery';
import { environment } from '../../../environments/environment';

@Component({
  templateUrl: 'profile.component.html',
  styleUrls: [ './profile.component.scss' ]
})

export class ProfileComponent implements OnInit {
  dataSource: any;
  user: IUser | null = { email: '', id: '' };
  nameEditorOptions: Object;
  buttonOptions: any = {
    text: 'Sauvegarder',
    type: 'success',
    useSubmitBehavior: true,
  };

  async ngOnInit() {
    this.user = (await this.authService.getUserInfo());
    this.dataSource = AspNetData.createStore({
      key: 'id',
      loadUrl: `${environment.apiUrl}/api/dog/${this.user?.id}`,
      insertUrl: `${environment.apiUrl}/api/dog/${this.user?.id}`,
      updateUrl: `${environment.apiUrl}/api/dog/${this.user?.id}`,
      deleteUrl: `${environment.apiUrl}/api/dog/${this.user?.id}`,
      onBeforeSend(method, ajaxOptions) {
        //ajaxOptions.xhrFields = { withCredentials: true };
        ajaxOptions.headers = {
          "Authorization": `Bearer ${localStorage.getItem('token')}`
        };
      },
    });
  }

  constructor(private authService: AuthService) {
    this.nameEditorOptions = { disabled: true };
  }
}
