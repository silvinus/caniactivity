declare var google: any;

import { CommonModule } from '@angular/common';
import { AfterViewChecked, AfterViewInit, Component, ElementRef, NgModule, OnInit, ViewChild } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { DxFormModule } from 'devextreme-angular/ui/form';
import { DxLoadIndicatorModule } from 'devextreme-angular/ui/load-indicator';
import notify from 'devextreme/ui/notify';
import { accounts } from 'google-one-tap';
import { AuthService } from '../../services';


@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.scss']
})
export class LoginFormComponent implements AfterViewInit {
  @ViewChild('gbutton') gbutton: ElementRef = new ElementRef({});
  loading = false;
  initialized = false;
  formData: any = {};

  constructor(private authService: AuthService, private router: Router) {
    //const gAccounts: accounts = google.accounts;
    //gAccounts.id.initialize({
    //  client_id: '525855556201-51in80imafjci7lkie1e8g671gbtbab0.apps.googleusercontent.com',
    //  ux_mode: 'popup',
    //  cancel_on_tap_outside: true,
    //  callback: (window as any).handleCredentialResponse
    //});
    //google.accounts.id.prompt();
    this.initialized = true;
  }
  ngAfterViewInit(): void {
    //setTimeout(() => {
    google.accounts.id.renderButton(this.gbutton.nativeElement, {
        size: 'large',
        class: 'g_id_signin',
        type: 'standard',
        shape: 'rectangular',
        theme: 'filled-blue',
        logo_alignement: 'left',
        width: 250,
      });
    //}, 10);
  }

  async onSubmit(e: Event) {
    e.preventDefault();
    const { email, password } = this.formData;
    this.loading = true;

    const result = await this.authService.logIn(email, password);
    if (!result.isOk) {
      this.loading = false;
      notify(result.message, 'error', 2000);
    }
  }

  onCreateAccountClick = () => {
    this.router.navigate(['/create-account']);
  }
}
@NgModule({
  imports: [
    CommonModule,
    RouterModule,
    DxFormModule,
    DxLoadIndicatorModule
  ],
  declarations: [ LoginFormComponent ],
  exports: [ LoginFormComponent ]
})
export class LoginFormModule { }
