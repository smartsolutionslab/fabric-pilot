import { expect, test } from 'vitest';

// AGENTS.md asks for an axe check in the component test of every new screen; a module's screen, <Name>Page.tsx, has
// that test beside it, <Name>Page.test.tsx.
const screens = Object.keys(import.meta.glob('./modules/**/*Page.tsx'));
const tests = import.meta.glob<string>('./modules/**/*Page.test.tsx', {
  query: '?raw',
  import: 'default',
  eager: true,
});

test('every module screen has an axe check in the component test beside it', () => {
  const unchecked = screens.filter(
    (screen) => !tests[screen.replace(/Page\.tsx$/, 'Page.test.tsx')]?.includes('toHaveNoViolations()'),
  );

  expect(unchecked, 'module screens without <Name>Page.test.tsx checking them with axe').toEqual([]);
});
