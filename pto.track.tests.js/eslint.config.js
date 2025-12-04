import js from "@eslint/js";
import jestPlugin from "eslint-plugin-jest";

export default [
    {
        files: ["**/*.js"],
        languageOptions: {
            ecmaVersion: 2021,
            sourceType: "module",
            globals: {
                window: "readonly",
                document: "readonly",
                console: "readonly",
                fetch: "readonly",
                jest: "readonly",
                describe: "readonly",
                it: "readonly",
                expect: "readonly",
                test: "readonly",
                beforeEach: "readonly",
                afterEach: "readonly",
                require: "readonly",
                module: "readonly",
                navigator: "readonly",
                Blob: "readonly",
                URL: "readonly",
                QUnit: "readonly"
            }
        },
        plugins: {
            jest: jestPlugin
        },
        rules: {
            ...js.configs.recommended.rules,
            ...jestPlugin.configs.recommended.rules,
            "no-unused-vars": "warn",
            "no-undef": "error",
            "semi": ["error", "always"],
            "quotes": ["error", "double"],
            "eqeqeq": "error"
        },
        ignores: [
            "node_modules/",
            "bin/",
            "obj/",
            "wwwroot/lib/",
            "*.min.js"
        ]
    }
];
