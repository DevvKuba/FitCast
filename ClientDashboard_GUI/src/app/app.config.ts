import { APP_INITIALIZER, ApplicationConfig, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MessageService } from 'primeng/api';
import { jwtInterceptorInterceptor } from './interceptors/jwt-interceptor.interceptor';
import { AccountService } from './services/account.service';

// "Precision Performance" - same palette as the Stitch design tokens in styles.css.
// Keep the primary/surface scales here in sync if the Stitch theme changes.
const FitCastPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#f0f7ff',
      100: '#dbecff',
      200: '#b8d9ff',
      300: '#85beff',
      400: '#479dff',
      500: '#0a7cff',
      600: '#0058be',
      700: '#004799',
      800: '#003c80',
      900: '#003066',
      950: '#001f42'
    },
    colorScheme: {
      light: {
        surface: {
          0: '#ffffff',
          50: '#f7f9fb',
          100: '#f2f4f6',
          200: '#eceef0',
          300: '#e6e8ea',
          400: '#e0e3e5',
          500: '#d8dadc',
          600: '#c2c6d6',
          700: '#727785',
          800: '#424754',
          900: '#191c1e',
          950: '#0c0d0e'
        }
      }
    }
  },
  components: {
    datatable: {
      headerCell: {
        background: '{surface.100}',
        color: '{surface.700}',
        // Applied automatically by PrimeNG (.p-datatable-column-sorted) to whichever
        // column is currently sorted - gives sorting a visible, built-in "active" state.
        selectedBackground: '{primary.100}',
        selectedColor: '{primary.700}'
      },
      columnTitle: {
        fontWeight: '700'
      }
    }
  }
});

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withInterceptors([jwtInterceptorInterceptor])),
    provideRouter(routes),
    provideAnimationsAsync(),
    MessageService,
    providePrimeNG({
      theme: {
        preset: FitCastPreset,
        options: {
          cssLayer: {
            name: 'primeng',
            order: 'theme, base, primeng'
          }
        }
      }
    }),
    provideAppInitializer(() => {
      const accountService = inject(AccountService);
      return accountService.initializeAuthState();
    })
  ]
};
