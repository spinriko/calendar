export default {
    testEnvironment: "jsdom",
    transform: {}, // disables Babel, use native ESM
    testMatch: ["**/*.test.mjs"],

    // Coverage configuration
    collectCoverageFrom: [
        "<rootDir>/../pto.track/wwwroot/js/calendar-functions.mjs",
        "!**/node_modules/**"
    ],
    coverageDirectory: "coverage",
    coverageReporters: ["text", "lcov", "html", "json-summary"],

    // Reporters configuration
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
    ]
};