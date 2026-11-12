import { skipToken } from '@reduxjs/toolkit/query';
import { Link, useParams } from 'react-router';
import { useGetKnowledgeArticlesByArticleIdQuery } from '@/api/generated';

/** One published article, its text in the paragraphs staff wrote it in. */
export function ArticlePage() {
  const { articleId } = useParams();
  const { data: article, isError } = useGetKnowledgeArticlesByArticleIdQuery(articleId ? { articleId } : skipToken);

  if (isError) {
    return (
      <main className="p-4">
        <p role="alert">This article could not be found.</p>
        <Link to="..">Back to the knowledge base</Link>
      </main>
    );
  }
  if (!article) {
    return (
      <main className="p-4">
        <p>Loading…</p>
      </main>
    );
  }

  return (
    <main className="mx-auto flex max-w-2xl flex-col gap-4 p-4">
      <Link to="..">Back to the knowledge base</Link>
      <h1 className="text-2xl font-semibold">{article.title}</h1>
      <p className="text-sm text-muted-foreground">Updated {new Date(article.changedAt).toLocaleDateString()}</p>
      {article.body.split(/\n\s*\n/).map((paragraph, position) => (
        // Paragraphs have no identity of their own; the text is replaced whole when it changes.
        <p key={position} className="whitespace-pre-wrap">
          {paragraph}
        </p>
      ))}
    </main>
  );
}
