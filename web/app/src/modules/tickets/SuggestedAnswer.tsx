import { useState } from 'react';
import { Link } from 'react-router';
import { type SuggestedAnswerDto, usePostKnowledgeSuggestedAnswersMutation } from '@/api/generated';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';

interface Props {
  /** What the answer answers: the ticket's subject and description. */
  question: string;
}

/**
 * An answer to the ticket drafted from the knowledge base, for staff to edit, with the articles it is drafted from.
 * The model's text is shown as plain text in the editor, never as markup, and nothing here sends it anywhere.
 */
export function SuggestedAnswer({ question }: Props) {
  const [suggest, { isLoading }] = usePostKnowledgeSuggestedAnswersMutation();
  const [suggestion, setSuggestion] = useState<SuggestedAnswerDto>();
  const [draft, setDraft] = useState('');
  const [failed, setFailed] = useState(false);

  const onSuggest = async () => {
    setFailed(false);
    try {
      const answer = await suggest({ suggestionRequest: { question } }).unwrap();
      setSuggestion(answer);
      setDraft(answer.drafted ? answer.text : '');
    } catch {
      setSuggestion(undefined);
      setFailed(true);
    }
  };

  return (
    <section aria-labelledby="suggested-answer" className="flex flex-col gap-3 rounded-md border p-4">
      <h2 id="suggested-answer" className="text-lg font-semibold">
        Suggested answer
      </h2>
      <p className="text-sm text-muted-foreground">
        Drafted from the knowledge base. It is not sent to the customer: read it, edit it and answer them yourself.
      </p>
      <div>
        <Button type="button" onClick={() => void onSuggest()} disabled={isLoading}>
          Suggest an answer
        </Button>
      </div>
      {failed ? (
        <p role="alert" className="text-sm text-destructive">
          No answer could be suggested.
        </p>
      ) : null}
      {suggestion && !suggestion.drafted ? <p role="status">{suggestion.text}</p> : null}
      {suggestion?.drafted ? (
        <>
          <div className="flex flex-col gap-2">
            <Label htmlFor="draft-answer">Draft answer</Label>
            <Textarea
              id="draft-answer"
              rows={8}
              value={draft}
              onChange={(event) => {
                setDraft(event.target.value);
              }}
            />
          </div>
          <h3 id="sources" className="font-medium">
            Sources
          </h3>
          <ul aria-labelledby="sources" className="list-disc pl-5">
            {suggestion.sources.map((source) => (
              <li key={source.articleId}>
                <Link to={`/knowledge/${source.articleId}`} className="underline">
                  {source.title}
                </Link>
              </li>
            ))}
          </ul>
        </>
      ) : null}
    </section>
  );
}
