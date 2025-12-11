module.exports = {
    preset: 'ts-jest/presets/default-esm',
    testEnvironment: 'jsdom',
    extensionsToTreatAsEsm: ['.ts'],
    // ts-jest options are defined per-transform to avoid the deprecated `globals` form
    // Map imports that resolve to the app's wwwroot JS files to the relative path
    // so Jest will transform them as project files rather than trying to load
    // them as raw Node modules.
    moduleNameMapper: {
        // Map any import that targets the app's wwwroot JS/TS files to the .ts source
        '.*wwwroot/js/(.*?)(?:\\.js)?$': '<rootDir>/../pto.track/wwwroot/js/$1.ts',
        // Map common intra-app relative imports (used inside the app sources) to their .ts source files
        '^(\\./|\\../)calendar-functions(?:\\.js)?$': '<rootDir>/../pto.track/wwwroot/js/calendar-functions.ts',
        '^(\\./|\\../)strategies/permission-strategies(?:\\.js)?$': '<rootDir>/../pto.track/wwwroot/js/strategies/permission-strategies.ts',
        // Preserve other relative .js imports
        '^(\\./|\\../)(.*)\\.js$': '$1$2'
    },
    transform: {
        '^.+\\.[tj]s$': ['ts-jest', { useESM: true, diagnostics: true }]
    },
    transformIgnorePatterns: ['/node_modules/']
};
