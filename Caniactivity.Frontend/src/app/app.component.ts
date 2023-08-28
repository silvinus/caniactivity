declare var google: any;

import { Component, HostBinding, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { accounts } from 'google-one-tap';
import { AuthService, ScreenService, AppInfoService } from './shared/services';

import frMessages from "devextreme/localization/messages/fr.json";
import { locale, loadMessages } from "devextreme/localization";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  @HostBinding('class') get getClass() {
    return Object.keys(this.screen.sizes).filter(cl => this.screen.sizes[cl]).join(' ');
  }

  constructor(private authService: AuthService,
    private screen: ScreenService, public appInfo: AppInfoService) {
    (window as any).handleCredentialResponse = (response: any) => this.authService.logInWithGoogle(response);
    loadMessages(frMessages);
    locale(navigator.language);
  }
  ngOnInit(): void {
    const gAccounts: accounts = google.accounts;
    gAccounts.id.initialize({
      client_id: '525855556201-51in80imafjci7lkie1e8g671gbtbab0.apps.googleusercontent.com',
      ux_mode: 'popup',
      cancel_on_tap_outside: true,
      callback: (window as any).handleCredentialResponse
    });
    }

  isAuthenticated() {
    return this.authService.loggedIn;
  }

  displayLoginForm() {
    return this.authService.displayLoginForm;
  }
}
