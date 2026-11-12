import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import {
  useGetKnowledgeArticlesManageByArticleIdQuery,
  usePostKnowledgeArticlesByArticleIdPublishMutation,
  usePostKnowledgeArticlesMutation,
  usePutKnowledgeArticlesByArticleIdMutation,
} from '@/api/generated';
import { ArticleEditorPage } from './ArticleEditorPage';

vi.mock('@/api/generated', () => ({
  useGetKnowledgeArticlesManageByArticleIdQuery: vi.fn(),
  usePostKnowledgeArticlesMutation: vi.fn(),
  usePutKnowledgeArticlesByArticleIdMutation: vi.fn(),
  usePostKnowledgeArticlesByArticleIdPublishMutation: vi.fn(),
}));

const write = vi.fn();
const edit = vi.fn();
const publish = vi.fn();

const draft = {
  id: 'd1',
  title: 'Printer drivers',
  body: 'Not ready yet.',
  state: 'Draft',
  changedAt: '2026-11-12T10:00:00+00:00',
  publishedAt: null as string | null,
};

function loaded(article: typeof draft | undefined) {
  vi.mocked(useGetKnowledgeArticlesManageByArticleIdQuery).mockReturnValue({
    data: article,
    isLoading: false,
    isError: false,
  } as never);
}

beforeEach(() => {
  for (const mutation of [write, edit, publish]) {
    mutation.mockReset();
    mutation.mockReturnValue({ unwrap: () => Promise.resolve() });
  }
  write.mockReturnValue({ unwrap: () => Promise.resolve('new-article-id') });
  vi.mocked(usePostKnowledgeArticlesMutation).mockReturnValue([write, { isLoading: false }] as never);
  vi.mocked(usePutKnowledgeArticlesByArticleIdMutation).mockReturnValue([edit, { isLoading: false }] as never);
  vi.mocked(usePostKnowledgeArticlesByArticleIdPublishMutation).mockReturnValue([
    publish,
    { isLoading: false },
  ] as never);
  loaded(draft);
});

afterEach(cleanup);

function renderEditor(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/knowledge/manage/new" element={<ArticleEditorPage />} />
        <Route path="/knowledge/manage/:articleId" element={<ArticleEditorPage />} />
        <Route path="/knowledge/manage" element={<h1>The article list</h1>} />
      </Routes>
    </MemoryRouter>,
  );
}

test('a new article is written with its title and text, then the list is shown', { timeout: 20_000 }, async () => {
  const user = userEvent.setup();
  renderEditor('/knowledge/manage/new');

  expect(screen.getByRole('heading', { name: 'Write an article' })).toBeInTheDocument();
  await user.type(screen.getByLabelText('Title'), 'Resetting a password');
  await user.type(screen.getByLabelText('Text'), 'Open the sign-in page.');
  await user.click(screen.getByRole('button', { name: 'Save' }));

  expect(await screen.findByRole('heading', { name: 'The article list' })).toBeInTheDocument();
  expect(write).toHaveBeenCalledWith({
    articleContent: { title: 'Resetting a password', body: 'Open the sign-in page.' },
  });
  expect(screen.queryByRole('button', { name: 'Publish' })).not.toBeInTheDocument();
});

test('an article without a title is refused next to the field, and nothing is sent', async () => {
  const user = userEvent.setup();
  renderEditor('/knowledge/manage/new');

  await user.type(screen.getByLabelText('Text'), 'Open the sign-in page.');
  await user.click(screen.getByRole('button', { name: 'Save' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('Give the article a title.');
  expect(screen.getByLabelText('Title')).toHaveAttribute('aria-invalid', 'true');
  expect(write).not.toHaveBeenCalled();
});

test("the API's refusal is shown next to the field it is about", async () => {
  const user = userEvent.setup();
  write.mockReturnValue({
    unwrap: () =>
      Promise.reject({
        data: { code: 'knowledge.invalid-body', title: 'An article needs a text of 1 to 20000 characters.' },
      }),
  });
  renderEditor('/knowledge/manage/new');

  await user.type(screen.getByLabelText('Title'), 'Title');
  await user.type(screen.getByLabelText('Text'), 'Text');
  await user.click(screen.getByRole('button', { name: 'Save' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('An article needs a text of 1 to 20000 characters.');
  expect(screen.getByLabelText('Text')).toHaveAttribute('aria-invalid', 'true');
});

test('an existing article is edited in place', async () => {
  const user = userEvent.setup();
  renderEditor('/knowledge/manage/d1');

  expect(await screen.findByRole('heading', { name: 'Edit an article' })).toBeInTheDocument();
  expect(screen.getByLabelText('Title')).toHaveValue('Printer drivers');
  expect(screen.getByText('Draft')).toBeInTheDocument();
  await user.clear(screen.getByLabelText('Text'));
  await user.type(screen.getByLabelText('Text'), 'Download them here.');
  await user.click(screen.getByRole('button', { name: 'Save' }));

  expect(await screen.findByRole('heading', { name: 'The article list' })).toBeInTheDocument();
  expect(useGetKnowledgeArticlesManageByArticleIdQuery).toHaveBeenCalledWith({ articleId: 'd1' });
  expect(edit).toHaveBeenCalledWith({
    articleId: 'd1',
    articleContent: { title: 'Printer drivers', body: 'Download them here.' },
  });
});

test('a draft is published from its editor', async () => {
  const user = userEvent.setup();
  renderEditor('/knowledge/manage/d1');

  await user.click(await screen.findByRole('button', { name: 'Publish' }));

  expect(publish).toHaveBeenCalledWith({ articleId: 'd1' });
});

test('a published article offers no publishing again', async () => {
  loaded({ ...draft, state: 'Published', publishedAt: '2026-11-12T11:00:00+00:00' });
  renderEditor('/knowledge/manage/d1');

  expect(await screen.findByText('Published')).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Publish' })).not.toBeInTheDocument();
});

test('the Article editor page has no accessibility violations axe can find', async () => {
  const { container } = renderEditor('/knowledge/manage/d1');
  expect(await screen.findByRole('heading', { name: 'Edit an article' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
