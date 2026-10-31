import js from '@eslint/js';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  {
    ignores: [
      'dist',
      'src/api/generated.ts'
    ],
  },
  {
    extends: [js.configs.recommended, ...tseslint.configs.strict],
    files: ['**/*.{ts,tsx}'],
    languageOptions: { globals: globals.browser },
    plugins: { 'react-hooks': reactHooks, 'react-refresh': reactRefresh },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
      'no-restricted-imports': ['error', {
        patterns: [
          { group: ['react-router-dom'], message: 'Use react-router (docs/adr).' },
          { group: ['zustand', 'mobx', 'mobx-react*', 'jotai', 'recoil'], message: 'Use the Redux store (docs/adr).' },
          { group: ['@mui/*', 'antd', '@mantine/*', '@chakra-ui/*'], message: 'Use the shadcn/ui components in src/components/ui (docs/adr).' },
          { group: ['formik', 'react-final-form', 'final-form'], message: 'Use react-hook-form (docs/adr).' },
          { group: ['yup', 'joi', 'valibot'], message: 'Use zod (docs/adr).' },
          { group: ['ag-grid-*'], message: 'Use DataTable from src/components/ui (docs/adr).' },
          { group: ['recharts', 'chart.js', 'react-chartjs-2'], message: 'Use Chart from src/components/chart (docs/adr).' }
        ],
      }],
    },
  },
);
