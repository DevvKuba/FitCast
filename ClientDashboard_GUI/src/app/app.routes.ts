import { Routes } from '@angular/router';
import { Home } from './home/home';
import { ClientWorkouts } from './client-workouts/client-workouts.component';
import { ClientInfoComponent } from './client-info/client-info.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { authGuard } from './guards/auth-guard.guard';
import { TrainerProfilePageComponent } from './trainer-profile-page/trainer-profile-page.component';
import { ClientPaymentsComponent } from './client-payments/client-payments.component';
import { ClientPersonalWorkoutsComponent } from './client-personal-workouts/client-personal-workouts.component';
import { ClientPersonalPaymentsComponent } from './client-personal-payments/client-personal-payments.component';

export const routes: Routes = [
  {path: '', component: Home},
  {path: 'client-info', component: ClientInfoComponent, canActivate: [authGuard]},
  {path: 'client-workouts', component: ClientWorkouts, canActivate: [authGuard]},
  {path: 'client-personal-workouts', component: ClientPersonalWorkoutsComponent, canActivate: [authGuard]},
  {path: 'client-payments', component: ClientPaymentsComponent, canActivate: [authGuard]},
  {path: 'client-personal-payments', component: ClientPersonalPaymentsComponent, canActivate: [authGuard]},

  // TODO change to analytics component when developing
  {path: 'client-analytics', component: ClientInfoComponent, canActivate: [authGuard]},
  {path: 'trainer-analytics', component: ClientInfoComponent, canActivate: [authGuard]},
  {path: 'login', component: LoginComponent},
  {path: 'register', component: RegisterComponent},
  {path: 'trainer-profile', component: TrainerProfilePageComponent, canActivate: [authGuard]}
];
