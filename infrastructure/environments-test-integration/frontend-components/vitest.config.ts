import { defineConfig } from 'vitest/config';
import { resolve } from 'path';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./setup.ts'],
    include: ['specs/**/*.spec.ts'],
    reporters: ['default', 'junit'],
    outputFile: { junit: 'coverage/junit.xml' },
    coverage: {
      provider: 'v8',
      reporter: ['html', 'text-summary'],
      reportsDirectory: 'coverage',
      include: ['src/**/*.ts'],
      exclude: [
        'src/**/*.spec.ts',
        'src/**/*.d.ts',
        'src/**/*.html',
        '**/*.html',
        'src/main.ts',
        'src/app/app.config.ts',
        'src/app/app.routes.ts',
        'src/app/**/*.routes.ts',
        'src/environments/**',
        'src/**/index.ts',
        'src/testing/**',
        'src/app/**/*-dialog.ts',
        'src/**/*.logic.ts',
        'src/**/*.validators.ts',
        'src/**/*-store.ts',
        'src/app/core/auth/**',
        'src/app/core/utils/**',
        'src/app/core/services/**',
        'src/app/core/router/**',
        'src/app/core/strategies/**',
        'src/app/shared/pipes/**',
        'src/app/shared/forms/validation-messages.ts',
      ],
    },
  },
  resolve: {
    alias: {
      '@core': resolve(__dirname, 'src/app/core'),
      '@shared': resolve(__dirname, 'src/app/shared'),
      '@pages': resolve(__dirname, 'src/app/pages'),
      '@env': resolve(__dirname, 'src/environments'),
      '@testing': resolve(__dirname, 'src/testing'),
    },
  },
});
