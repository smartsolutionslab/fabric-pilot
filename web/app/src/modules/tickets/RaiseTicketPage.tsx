import { zodResolver } from '@hookform/resolvers/zod';
import { type FieldPath, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router';
import { usePostTicketsTicketsMutation } from '@/api/generated';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { NativeSelect } from '@/components/ui/native-select';
import { Textarea } from '@/components/ui/textarea';
import { priorities, type RaiseTicketForm, raiseTicketSchema } from './raiseTicketSchema';

/** The field each refusal of the API is about; anything else is shown above the form. */
const fieldOf: Record<string, FieldPath<RaiseTicketForm>> = {
  'tickets.invalid-subject': 'subject',
  'tickets.invalid-description': 'description',
  'tickets.unknown-priority': 'priority',
  'tickets.too-many-attachments': 'attachments',
  'tickets.attachment-too-large': 'attachments',
  'tickets.invalid-attachment-name': 'attachments',
};

interface Problem {
  data?: { code?: string; title?: string };
}

export function RaiseTicketPage() {
  const navigate = useNavigate();
  const [raise, { isLoading }] = usePostTicketsTicketsMutation();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(raiseTicketSchema),
    defaultValues: { subject: '', description: '', priority: 'Normal' },
  });

  const onSubmit = handleSubmit(async (ticket) => {
    try {
      await raise({ body: ticket }).unwrap();
      await navigate('..', { relative: 'path' });
    } catch (failure) {
      const { code, title } = (failure as Problem).data ?? {};
      setError(fieldOf[code ?? ''] ?? 'root', { message: title ?? 'The ticket could not be raised.' });
    }
  });

  const describedBy = (field: FieldPath<RaiseTicketForm>) => (errors[field] ? `${field}-error` : undefined);
  const fieldError = (field: FieldPath<RaiseTicketForm>) =>
    errors[field] ? (
      <p id={`${field}-error`} role="alert" className="text-sm text-destructive">
        {errors[field].message}
      </p>
    ) : null;

  return (
    <main className="mx-auto flex max-w-2xl flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">Raise a ticket</h1>
      {errors.root ? (
        <p role="alert" className="text-sm text-destructive">
          {errors.root.message}
        </p>
      ) : null}
      <form noValidate onSubmit={(event) => void onSubmit(event)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <Label htmlFor="subject">Subject</Label>
          <Input
            id="subject"
            aria-invalid={Boolean(errors.subject)}
            aria-describedby={describedBy('subject')}
            {...register('subject')}
          />
          {fieldError('subject')}
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="description">Description</Label>
          <Textarea
            id="description"
            rows={6}
            aria-invalid={Boolean(errors.description)}
            aria-describedby={describedBy('description')}
            {...register('description')}
          />
          {fieldError('description')}
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="priority">Priority</Label>
          <NativeSelect
            id="priority"
            aria-invalid={Boolean(errors.priority)}
            aria-describedby={describedBy('priority')}
            {...register('priority')}
          >
            {priorities.map((priority) => (
              <option key={priority} value={priority}>
                {priority}
              </option>
            ))}
          </NativeSelect>
          {fieldError('priority')}
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="attachments">Attachments</Label>
          <Input
            id="attachments"
            type="file"
            multiple
            aria-invalid={Boolean(errors.attachments)}
            aria-describedby={describedBy('attachments')}
            {...register('attachments')}
          />
          {fieldError('attachments')}
        </div>
        <Button type="submit" disabled={isLoading} className="self-start">
          Raise ticket
        </Button>
      </form>
    </main>
  );
}
