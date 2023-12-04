import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { LoginFormComponent, ResetPasswordFormComponent, CreateAccountFormComponent, ChangePasswordFormComponent } from './shared/components';
import { AuthGuardService, JwtInterceptor } from './shared/services';
import { HomeComponent } from './pages/home/home.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { DxAccordionModule, DxBoxModule, DxButtonModule, DxDataGridModule, DxFileManagerModule, DxFormModule, DxPopupModule, DxResponsiveBoxModule, DxSchedulerModule, DxTabPanelModule, DxTemplateModule, DxTileViewModule } from 'devextreme-angular';
import { EnvironmentComponent } from './pages/environment/environment.component';
import { ActivitiesComponent } from './pages/activities/activities.component';
import { ScheduleComponent } from './pages/schedule/schedule.component';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { UsersComponent } from './pages/users/users.component';
import { SchedulerAdminComponent } from './pages/scheduler-admin/scheduler-admin.component';
import { TarifsComponent } from './pages/tarifs/tarifs.component';
import { CguComponent } from './pages/cgu/cgu.component';
import { PrivacyComponent } from './pages/privacy/privacy.component';
import { PartnersComponent } from './pages/partners/partners.component';
import { PhotosComponent } from './pages/photos/photos.component';
import { FilesComponent } from './pages/files/files.component';
import { FacebookLoginProvider, SocialAuthServiceConfig, SocialLoginModule } from '@abacritt/angularx-social-login';

const routes: Routes = [
  {
    path: 'pages/files',
    component: FilesComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/photos',
    component: PhotosComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/partners',
    component: PartnersComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/privacy',
    component: PrivacyComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/cgu',
    component: CguComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/tarifs',
    component: TarifsComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/scheduler-admin',
    component: SchedulerAdminComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/users',
    component: UsersComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/schedule',
    component: ScheduleComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/activities',
    component: ActivitiesComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'pages/environment',
    component: EnvironmentComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'profile',
    component: ProfileComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'home',
    component: HomeComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'login-form',
    component: LoginFormComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'reset-password',
    component: ResetPasswordFormComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'create-account',
    component: CreateAccountFormComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: 'change-password/:recoveryCode',
    component: ChangePasswordFormComponent,
    canActivate: [ AuthGuardService ]
  },
  {
    path: '**',
    redirectTo: 'home'
  }
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes, { useHash: true }),
    DxDataGridModule,
    DxFormModule,
    DxBoxModule,
    DxButtonModule,
    BrowserModule,
    DxSchedulerModule,
    DxTemplateModule,
    DxAccordionModule,
    DxFileManagerModule,
    DxPopupModule,
    DxTileViewModule,
    DxTabPanelModule,
    SocialLoginModule,
    DxResponsiveBoxModule
  ],
  providers: [
    AuthGuardService,
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
    {
      provide: 'SocialAuthServiceConfig',
      useValue: {
        autoLogin: false,
        providers: [
          {
            id: FacebookLoginProvider.PROVIDER_ID,
            provider: new FacebookLoginProvider('1415956495802529')
          }
        ],
        onError: (err) => {
          console.error(err);
        }
      } as SocialAuthServiceConfig,
    }
  ],
  exports: [RouterModule],
  declarations: [
    HomeComponent,
    ProfileComponent,
    EnvironmentComponent,
    ActivitiesComponent,
    ScheduleComponent,
    UsersComponent,
    SchedulerAdminComponent,
    TarifsComponent,
    CguComponent,
    PrivacyComponent,
    PartnersComponent,
    PhotosComponent,
    FilesComponent
  ]
})
export class AppRoutingModule { }
