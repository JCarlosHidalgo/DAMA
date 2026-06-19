import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['src/testing/setup/vitest-setup.ts'],
    include: ['src/**/*.spec.ts'],
    reporters: ['default', 'junit'],
    outputFile: {
      junit: 'coverage/junit.xml',
    },
    coverage: {
      provider: 'v8',
      reporter: ['html', 'cobertura', 'text-summary'],
      reportsDirectory: 'coverage',
      excludeAfterRemap: true,
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
        'src/app/pages/**/!(*.logic|*.validators|*.variants|*-store).ts',
        'src/app/**/*-dialog.ts',
      ],
    },
  },
});
