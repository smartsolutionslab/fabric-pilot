import { Route, Routes } from 'react-router';
import { ArticleEditorPage } from './ArticleEditorPage';
import { ArticlePage } from './ArticlePage';
import { KnowledgePage } from './KnowledgePage';
import { ManageArticlesPage } from './ManageArticlesPage';

export function KnowledgeRoutes() {
  return (
    <Routes>
      <Route index element={<KnowledgePage />} />
      <Route path="manage" element={<ManageArticlesPage />} />
      <Route path="manage/new" element={<ArticleEditorPage />} />
      <Route path="manage/:articleId" element={<ArticleEditorPage />} />
      <Route path=":articleId" element={<ArticlePage />} />
    </Routes>
  );
}
