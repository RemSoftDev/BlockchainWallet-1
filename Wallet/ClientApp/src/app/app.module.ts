import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { ReCaptchaModule } from 'angular2-recaptcha';
import { CookieModule } from 'ngx-cookie';
import { SimpleNotificationsModule } from 'angular2-notifications';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { JwtInterceptor } from './jwt.interceptor';
import { AppComponent } from './app.component';
import { NavMenuComponent } from './components/nav-menu/nav-menu.component';
import { HomeComponent } from './components/home/home.component';
import { FooterMenuComponent } from './components/footer-menu/footer-menu.component';
import { LoginComponent } from './components/Login/login.component';
import { SigninComponent } from './components/sign-in/signin.component';
import { WalletPageComponent } from './components/wallet-page/wallet-page.component';
import { SpinnerComponent } from './components/spinner/spinner.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { AboutComponent } from './components/about/about.component';
import { ContactComponent } from './components/contact/contact.component';
import { WatchlistComponent } from './components/watchlist/watchlist.component';
import { ContractPageComponent } from './components/contract-page/contract-page.component';
import { AccountPageComponent } from './components/account-page/account-page.component';

import { AuthService } from './shared/services/auth.service';
import { RedirectionService } from './shared/services/redirection.service';
import { BlockchainService } from './shared/services/blockchain.service';
import { PageDataService } from './shared/services/pageData.service';
import { AdminPanelComponent } from './components/admin-panel/admin-panel.component';
import { AuthAdminGuardService } from './shared/services/auth-admin-guard.service';
import { AuthUserGuardService } from './shared/services/auth-user-guard.service';
import { WatchlistService } from './shared/services/watchlist.service';
import { NotificationService } from './shared/services/notifications.service';
import { TokenService } from './shared/services/adminToken.service';
import { SortingWatchlistPipe } from './pipe/sort-watchlist';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    FooterMenuComponent,
    LoginComponent,
    SigninComponent,
    WalletPageComponent,
    SpinnerComponent,
    ForgotPasswordComponent,
    ResetPasswordComponent,
    AboutComponent,
    ContactComponent,
    WatchlistComponent,
    AdminPanelComponent,
    ContractPageComponent,
    AccountPageComponent,
    SortingWatchlistPipe
  ],
  imports: [
    CookieModule.forRoot(),
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    ReCaptchaModule,
    BrowserAnimationsModule,
    SimpleNotificationsModule.forRoot(),
    FormsModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'search/:searchString', component: WalletPageComponent, runGuardsAndResolvers: 'always' },
      { path: 'search/contract/:searchString', component: ContractPageComponent, runGuardsAndResolvers: 'always' },
      { path: 'search/account/:searchString', component: AccountPageComponent, runGuardsAndResolvers: 'always' },
      { path: 'sign-in', component: SigninComponent }, 
      { path: 'forgot-password', component: ForgotPasswordComponent },
      { path: 'Account/ResetPassword', component: ResetPasswordComponent },
      { path: 'api/Account/ResetPassword', component: ResetPasswordComponent },
      { path: 'log-in', component: LoginComponent },
      { path: 'log-in/ConfirmEmail', component: LoginComponent },
      { path: 'api/Account/ConfirmEmail', component: LoginComponent },
      { path: 'Account/ConfirmEmail', component: LoginComponent },
      { path: 'admin-panel', component: AdminPanelComponent, canActivate: [AuthAdminGuardService] },
      { path: 'about', component: AboutComponent },
      { path: 'contact', component: ContactComponent },
      { path: 'watchlist', component: WatchlistComponent, canActivate: [AuthUserGuardService] }
    ],
      { onSameUrlNavigation: 'reload' })
  ],
  providers: [
    AuthAdminGuardService,
    AuthUserGuardService,
    BlockchainService,
    RedirectionService,
    PageDataService,
    WatchlistService,
    NotificationService,
    TokenService,
    AuthService, {
      provide: HTTP_INTERCEPTORS,
      useClass: JwtInterceptor,
      multi: true
    }
    ],
  bootstrap: [AppComponent]
})
export class AppModule { }
