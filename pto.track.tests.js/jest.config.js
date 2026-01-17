/** @type {import('ts-jest').JestConfigWithTsJest} */
export default {
    preset: "ts-jest",
    testEnvironment: "jsdom",
    testTimeout: 30000,
    testMatch: ["**/tests/**/*.test.ts"],

    transform: {
        "^.+\\.m?[tj]sx?$": ["ts-jest", {
            useESM: true,
        }],
    },

    collectCoverageFrom: [
        "<rootDir>/../pto.track/wwwroot/js/calendar-functions.ts",
        "<rootDir>/../pto.track/wwwroot/js/absences-scheduler.ts",
        "!**/node_modules/**"
    ],
    coverageDirectory: "coverage",
    coverageReporters: ["text", "lcov", "html", "json-summary"],

    reporters: [
        "default",
        ["jest-junit", {
            outputDirectory: "./test-results",
            outputName: "jest-junit.xml",
            ancestorSeparator: " › ",
            uniqueOutputName: false,
            suiteNameTemplate: "{filepath}",
            classNameTemplate: "{classname}",
            titleTemplate: "{title}"
        }]
    ],

    extensionsToTreatAsEsm: [".ts"],
    moduleNameMapper: {
        // Map any import that targets the app's wwwroot JS/TS files to the .ts source
        ".*wwwroot/js/(.*?)(?:\\.js)?$": "<rootDir>/../pto.track/wwwroot/js/$1.ts",
        // Map common intra-app relative imports (used inside the app sources) to their .ts source files
        "^(\\./|\\../)calendar-functions(?:\\.js)?$": "<rootDir>/../pto.track/wwwroot/js/calendar-functions.ts",
        "^(\\./|\\../)strategies/permission-strategies(?:\\.js)?$": "<rootDir>/../pto.track/wwwroot/js/strategies/permission-strategies.ts",
        // Preserve other relative .js imports
        "^(\\./|\\../)(.*)\\.js$": "$1$2"
    }
};
