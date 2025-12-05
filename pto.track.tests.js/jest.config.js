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
      "<rootDir>/../pto.track/wwwroot/js/calendar-functions.mjs",
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
    "^(\\.{1,2}/.*)\\.js$": "$1",
    "^(\\.{1,2}/.*)\\.mjs$": "$1"
  }
};
