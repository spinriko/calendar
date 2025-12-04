module.exports = {
    env: {
        browser: true,
        es2021: true,
        node: true,
        jest: true
    },
    extends: [
        "eslint:recommended",
        "plugin:jest/recommended"
    ],
    plugins: ["jest"],
    parserOptions: {
        ecmaVersion: 12,
        sourceType: "module"
    },
    rules: {
        "no-unused-vars": "warn",
        "no-undef": "error",
        "semi": ["error", "always"],
        "quotes": ["error", "double"],
        "eqeqeq": "error"
    },
    ignorePatterns: [
        "node_modules/",
        "bin/",
        "obj/",
        "wwwroot/lib/",
        "*.min.js"
    ]
};
