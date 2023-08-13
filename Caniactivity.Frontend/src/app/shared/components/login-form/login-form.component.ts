declare var google: any;

import { CommonModule } from '@angular/common';
import { AfterViewChecked, Component, NgModule } from '@angular/core';
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
export class LoginFormComponent implements AfterViewChecked {
  loading = false;
  initialized = false;
  formData: any = {};

  constructor(private authService: AuthService, private router: Router) {
    (window as any).handleCredentialResponse = (response: any) => this.authService.logInWithGoogle(response)
  }
  ngAfterViewChecked(): void {
    if (!this.initialized) {
      const gAccounts: accounts = google.accounts;
      gAccounts.id.initialize({
        client_id: '525855556201-51in80imafjci7lkie1e8g671gbtbab0.apps.googleusercontent.com',
        ux_mode: 'popup',
        cancel_on_tap_outside: true,
        callback: (window as any).handleCredentialResponse
      });
      //google.accounts.id.prompt();
      gAccounts.id.renderButton(document.getElementById('g_id_signin') as HTMLElement, {
        size: 'large',
        width: 280,
      });
      this.initialized = true;
    }
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
