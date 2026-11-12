import { zodResolver } from '@hookform/resolvers/zod';
import { skipToken } from '@reduxjs/toolkit/query';
import { useEffect } from 'react';
import { type FieldPath, useForm } from 'react-hook-form';
import { Link, useNavigate, useParams } from 'react-router';
import {
  useGetKnowledgeArticlesManageByArticleIdQuery,
  usePostKnowledgeArticlesByArticleIdPublishMutation,
  usePostKnowledgeArticlesMutation,
  usePutKnowledgeArticlesByArticleIdMutation,
} from '@/api/generated';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { type ArticleForm, articleSchema } from './articleSchema';

/** The field each refusal of the API is about; anything else is shown above the form. */
const fieldOf: Record<string, FieldPath<ArticleForm>> = {
  'knowledge.invalid-title': 'title',
  'knowledge.invalid-body': 'body',
};

interface Problem {
  data?: { code?: string; title?: string };
}

/** Staff write a new article or edit one, and publish a draft to every tenant. */
export function ArticleEditorPage() {
  const { articleId } = useParams();
  const navigate = useNavigate();
  const { data } = useGetKnowledgeArticlesManageByArticleIdQuery(articleId ? { articleId } : skipToken);
  // A new article starts empty.
  const article = articleId ? data : undefined;
  const [write, { isLoading: isWriting }] = usePostKnowledgeArticlesMutation();
  const [edit, { isLoading: isEditing }] = usePutKnowledgeArticlesByArticleIdMutation();
  const [publish, { isLoading: isPublishing }] = usePostKnowledgeArticlesByArticleIdPublishMutation();
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm({ resolver: zodResolver(articleSchema), defaultValues: { title: '', body: '' } });

  useEffect(() => {
    if (article) {
      reset({ title: article.title, body: article.body });
    }
  }, [article, reset]);

  const refused = (failure: unknown, fallback: string) => {
    const { code, title } = (failure as Problem).data ?? {};
    setError(fieldOf[code ?? ''] ?? 'root', { message: title ?? fallback });
  };

  const onSubmit = handleSubmit(async (content) => {
    try {
      if (articleId) {
        await edit({ articleId, body: content }).unwrap();
      } else {
        await write({ body: content }).unwrap();
      }
      await navigate('/knowledge/manage');
    } catch (failure) {
      refused(failure, 'The article could not be saved.');
    }
  });

  const onPublish = async () => {
    if (!articleId) {
      return;
    }
    try {
      await publish({ articleId }).unwrap();
    } catch (failure) {
      refused(failure, 'The article could not be published.');
    }
  };

  const describedBy = (field: FieldPath<ArticleForm>) => (errors[field] ? `${field}-error` : undefined);
  const fieldError = (field: FieldPath<ArticleForm>) =>
    errors[field] ? (
      <p id={`${field}-error`} role="alert" className="text-sm text-destructive">
        {errors[field].message}
      </p>
    ) : null;

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <Link to="/knowledge/manage">Back to the articles</Link>
      <h1 className="text-2xl font-semibold">{articleId ? 'Edit an article' : 'Write an article'}</h1>
      {article ? (
        <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
          <dt className="font-medium">State</dt>
          <dd>{article.state}</dd>
        </dl>
      ) : null}
      {errors.root ? (
        <p role="alert" className="text-sm text-destructive">
          {errors.root.message}
        </p>
      ) : null}
      <form noValidate onSubmit={(event) => void onSubmit(event)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <Label htmlFor="title">Title</Label>
          <Input id="title" aria-invalid={Boolean(errors.title)} aria-describedby={describedBy('title')} {...register('title')} />
          {fieldError('title')}
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="body">Text</Label>
          <Textarea
            id="body"
            rows={16}
            aria-invalid={Boolean(errors.body)}
            aria-describedby={describedBy('body')}
            {...register('body')}
          />
          {fieldError('body')}
        </div>
        <div className="flex gap-2">
          <Button type="submit" disabled={isWriting || isEditing}>
            Save
          </Button>
          {article?.state === 'Draft' ? (
            <Button type="button" variant="outline" disabled={isPublishing} onClick={() => void onPublish()}>
              Publish
            </Button>
          ) : null}
        </div>
      </form>
    </main>
  );
}
