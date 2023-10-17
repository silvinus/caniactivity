import { Component, Input, OnInit } from '@angular/core';
import { AppInfoService, AuthService, IUser } from '../../shared/services';
import * as AspNetData from 'devextreme-aspnet-data-nojquery';
import { environment } from '../../../environments/environment';
import { UserService } from '../../shared/services/user.service';

@Component({
  selector: 'profile-view',
  templateUrl: 'profile.component.html',
  styleUrls: [ './profile.component.scss' ]
})

export class ProfileComponent implements OnInit {
  dataSource: any;
  @Input() user: any | null = { email: '', id: '' };
  nameEditorOptions: Object = { disabled: true };
  statusEditorOptions: any = null;
  providerEditorOptions: any = null;
  dogEditorOptions: any = null;
  dogStatuses: any;
  isAdmin: boolean = false;

  buttonOptions: any = {
    text: 'Sauvegarder',
    type: 'success',
    useSubmitBehavior: true,
    onClick: async () => {
      await this.userService.updateUser(this.user!.id, {
        firstName: this.user.firstName,
        lastName: this.user.lastName,
        phone: this.user.phone
      });
    }
  };

  buttonOptionsDelete: any = {
    text: 'Supprimer',
    type: 'danger',
    useSubmitBehavior: true,
    onClick: async () => {
      await this.userService.deleteUser(this.user!.id);
    }
  };

  async ngOnInit() {
    if (this.user?.id === undefined || this.user?.id === "") {
      this.user = (await this.authService.getUserInfo());
    }
    else {
      this.isAdmin = true;
    }
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
    this.providerEditorOptions = { disabled: true, dataSource: this.infoService.providers, valueExpr: 'id', displayExpr: 'name' };
    this.statusEditorOptions = { disabled: !this.isAdmin, dataSource: this.infoService.statuses, valueExpr: 'id', displayExpr: 'name' };
    this.dogEditorOptions = { disabled: !this.isAdmin };
  }

  constructor(private authService: AuthService, private infoService: AppInfoService, private userService: UserService) {
    this.dogStatuses = infoService.dogStatuses;
  }
}
