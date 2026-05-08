export default [
    {
        files: ['.github/scripts/**/*.mjs'],
        languageOptions: {
            ecmaVersion: 'latest',
            sourceType: 'module',
            globals: {
                console: 'readonly',
                fetch: 'readonly',
                process: 'readonly',
            },
        },
        rules: {
            eqeqeq: ['error', 'always'],
            'no-undef': 'error',
            'no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
            'no-var': 'error',
            'prefer-const': 'error',
        },
    },
];
