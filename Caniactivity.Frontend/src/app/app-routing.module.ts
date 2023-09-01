import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { LoginFormComponent, ResetPasswordFormComponent, CreateAccountFormComponent, ChangePasswordFormComponent } from './shared/components';
import { AuthGuardService, JwtInterceptor } from './shared/services';
import { HomeComponent } from './pages/home/home.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { DxBoxModule, DxButtonModule, DxDataGridModule, DxFormModule, DxSchedulerModule, DxTemplateModule } from 'devextreme-angular';
import { EnvironmentComponent } from './pages/environment/environment.component';
import { ActivitiesComponent } from './pages/activities/activities.component';
import { ScheduleComponent } from './pages/schedule/schedule.component';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { UsersComponent } from './pages/users/users.component';
import { SchedulerAdminComponent } from './pages/scheduler-admin/scheduler-admin.component';
import { TarifsComponent } from './pages/tarifs/tarifs.component';

const routes: Routes = [
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
    DxTemplateModule
  ],
  providers: [
    AuthGuardService,
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true }],
  exports: [RouterModule],
  declarations: [
    HomeComponent,
    ProfileComponent,
    EnvironmentComponent,
    ActivitiesComponent,
    ScheduleComponent,
    UsersComponent,
    SchedulerAdminComponent,
    TarifsComponent
  ]
})
export class AppRoutingModule { }
