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

// "Precision Performance" - same primary palette as the Stitch design tokens in styles.css.
// Keep this in sync if the Stitch theme changes.
//
// Deliberately NOT overriding semantic.colorScheme.surface here. Aura uses that scale as a
// general-purpose neutral ramp - text, icons, borders AND backgrounds all read from it across
// every component (button icons, menu items, table borders, sort icons, etc.), not just card
// backgrounds. An earlier attempt replaced the whole scale with Stitch's literal background
// hexes (which are deliberately very pale) and made every muted icon/text element in the app
// render almost invisibly. Aura's default surface scale (Tailwind's slate palette) is close
// enough to Stitch's own greys to look right, and keeps every component's text/icon colour
// legible without per-token patching.
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
