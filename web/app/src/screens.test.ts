import { expect, test } from 'vitest';

// Every module screen, <Name>Page.tsx, has its axe check beside it in <Name>Page.axe.test.tsx (AGENTS.md): add-module
// generates it with a module's page, and a screen written by hand gets one too. An axe assertion in another test does
// not count, since it can be skipped or commented out.
const screens = Object.keys(import.meta.glob('./modules/**/*Page.tsx'));
const checks = import.meta.glob<string>('./modules/**/*Page.axe.test.tsx', {
  query: '?raw',
  import: 'default',
  eager: true,
});

test('every module screen has an axe check beside it', () => {
  const unchecked = screens.filter(
    (screen) => !checks[screen.replace(/Page\.tsx$/, 'Page.axe.test.tsx')]?.includes('toHaveNoViolations()'),
  );

  expect(unchecked, 'module screens without <Name>Page.axe.test.tsx checking them with axe').toEqual([]);
});
