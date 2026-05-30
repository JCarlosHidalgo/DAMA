import { EnvironmentProviders, Provider, Type, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

export interface StandaloneTestBedOptions {
  imports?: (Type<unknown> | unknown)[];
  providers?: (Provider | EnvironmentProviders)[];
  includeRouter?: boolean;
}

export async function configureStandaloneTestBed(
  options: StandaloneTestBedOptions = {},
): Promise<void> {
  const includeRouter = options.includeRouter ?? true;
  const providers: (Provider | EnvironmentProviders)[] = [
    provideZonelessChangeDetection(),
    ...(options.providers ?? []),
  ];
  if (includeRouter) {
    providers.push(provideRouter([]));
  }
  await TestBed.configureTestingModule({
    imports: options.imports ?? [],
    providers,
  }).compileComponents();
}
