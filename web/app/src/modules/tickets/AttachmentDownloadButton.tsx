import { useState } from 'react';
import { type AttachmentDto, useLazyGetTicketsTicketsByTicketIdAttachmentsAndAttachmentIdQuery } from '@/api/generated';
import { Button } from '@/components/ui/button';

/** Fetches the file with the user's token, then hands it to the browser to save under its own name. */
export function AttachmentDownloadButton({ ticketId, attachment }: { ticketId: string; attachment: AttachmentDto }) {
  const [download] = useLazyGetTicketsTicketsByTicketIdAttachmentsAndAttachmentIdQuery();
  const [saving, setSaving] = useState(false);
  const [failed, setFailed] = useState(false);

  async function save() {
    setFailed(false);
    setSaving(true);
    try {
      const content = await download({ ticketId, attachmentId: attachment.id }).unwrap();
      const url = URL.createObjectURL(content as Blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = attachment.fileName;
      document.body.append(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    } catch {
      setFailed(true);
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <Button variant="outline" size="sm" disabled={saving} onClick={() => void save()}>
        Download {attachment.fileName}
      </Button>
      {failed ? <span role="alert">{attachment.fileName} could not be downloaded.</span> : null}
    </>
  );
}
