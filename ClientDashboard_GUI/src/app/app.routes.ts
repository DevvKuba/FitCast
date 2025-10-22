import { Routes } from '@angular/router';
import { Home } from './home/home';
import { ClientWorkouts } from './client-workouts/client-workouts.component';
import { ClientInfoComponent } from './client-info/client-info.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { authGuard } from './guards/auth-guard.guard';

export const routes: Routes = [
  {path: '', component: Home},
  {path: 'client-workouts', component: ClientWorkouts, canActivate: [authGuard]},
  {path: 'client-info', component: ClientInfoComponent, canActivate: [authGuard]},
  {path: 'login', component: LoginComponent},
  {path: 'register', component: RegisterComponent},
];
