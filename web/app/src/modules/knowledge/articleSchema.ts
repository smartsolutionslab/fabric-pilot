import { z } from 'zod';

/** The article's own limits, as the API checks them (Article in the knowledge module). */
export const articleSchema = z.object({
  title: z.string().trim().min(1, 'Give the article a title.').max(200, 'Keep the title to 200 characters.'),
  body: z.string().trim().min(1, 'Write the text of the article.').max(20000, 'Keep the text to 20000 characters.'),
});

export type ArticleForm = z.input<typeof articleSchema>;
