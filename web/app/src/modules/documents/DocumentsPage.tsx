import { Inbox } from 'lucide-react';
import type { DocumentsSampleDto } from '@/api/generated';
import { Button } from '@/components/ui/button';
import { DataTable } from '@/components/ui/data-table';
import { columnsOf } from '@/components/ui/data-table-columns';
import { EmptyState } from '@/components/ui/empty-state';
import { notify } from '@/lib/notify';
import { CreateDocumentsSampleForm, sampleNameField } from './CreateDocumentsSampleForm';
import { useCanManage } from './useCanManage';
import { useCreateDocumentsSample, useDocumentsSamples } from './useDocumentsSamples';

const samples = columnsOf<DocumentsSampleDto>();
const columns = samples.columns([samples.accessor('name', { header: 'Name' })]);
const none: DocumentsSampleDto[] = [];

/** The empty state's action: the form's name field, where the first sample is created. */
function toTheForm() {
  document.getElementById(sampleNameField)?.focus();
}

/**
 * The module's screen, starting from its sample (AGENTS.md): the samples in a table, by name, and a form that creates
 * one, shown in the table at once and taken out again if the host refuses it. "Manage" shows only to a
 * caller its permission probe answers with 200, and any failure but a 403 shows as an alert.
 */
export function DocumentsPage() {
  const { samples: rows, page, isLoading, isError } = useDocumentsSamples();
  const { create, isSaving } = useCreateDocumentsSample();
  const permission = useCanManage();

  async function onCreate(name: string) {
    const created = await create(name);
    if (created) {
      notify.success(`The sample ${name} was created.`);
    } else {
      notify.failure(`The sample ${name} could not be created.`);
    }
    return created;
  }

  const empty = (
    <EmptyState
      icon={<Inbox />}
      action={
        <Button type="button" variant="outline" onClick={toTheForm}>
          Create a sample
        </Button>
      }
    >
      No samples yet.
    </EmptyState>
  );

  return (
    <main className="flex flex-col gap-6">
      <h1>Documents</h1>
      {permission === 'granted' && <h2>Manage</h2>}
      {permission === 'failed' && <p role="alert">Permissions for documents could not be loaded.</p>}
      <section aria-labelledby="documents-samples" className="flex flex-col gap-4">
        <h2 id="documents-samples">Samples</h2>
        <CreateDocumentsSampleForm onCreate={onCreate} saving={isSaving} />
        {isError ? (
          <p role="alert">The samples could not be loaded.</p>
        ) : (
          <DataTable columns={columns} data={rows ?? none} loading={isLoading} empty={empty} paging={page} />
        )}
      </section>
    </main>
  );
}
