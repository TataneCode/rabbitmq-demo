import { ApplicationConfig } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';

// provideHttpClient rend HttpClient disponible pour injection dans tous les composants
export const appConfig: ApplicationConfig = {
  providers: [provideHttpClient()],
};
