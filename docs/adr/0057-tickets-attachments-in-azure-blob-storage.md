# Tickets: attachments in Azure Blob Storage, one container, Azurite locally

**Status:** Accepted. It records what the pilot built by hand for charter feature 2 (#6, PR #24), as the owner's decision X2 on software-fabric#602 (2026-11-21) asks (#90). A pilot decision, not generated. It informs software-fabric ADR-0020 (`docs/adr/0020-blob-storage.md`, `backend.blob-storage`). It describes `main` as it is; #89 (open, branch `agent/89`) changes the blob names so that each starts with its tenant.

## Context

A customer raises a ticket with a subject, a description, a priority and files (charter feature 2, #6). The charter's "Where it runs" names Blob Storage "for documents and attachments", and the catalog has no option for it, so the charter lists document and blob storage under "Hand-written here, not generated", as an M5 catalog candidate. Issue #6 says "Azurite locally, Azure Blob Storage when deployed"; the owner's decision on #10 (2026-11-12) repeats "Blob storage is Azurite locally". Why Azure Blob Storage rather than another store follows from the charter's Azure deployment; nothing else is recorded.

Only the tickets module stores files on `main`. The documents module has no blob storage of its own, and invoice PDFs are rendered on request, not stored (docs/adr/0059).

## What was built

- **The interface**, owned by the module's Application project: `src/Modules/Tickets/Tickets.Application/IBlobStore.cs`, with `UploadAsync(name, content)` ("replacing what was stored there") and `OpenReadAsync(name)`, which returns `Option<Stream>`. Its comment calls it "the one place a blob storage pack would replace".
- **The store**, `src/Modules/Tickets/Tickets.Infrastructure/Storage/AzureBlobStore.cs`, over `Azure.Storage.Blobs`: one container, `ticket-attachments`, named in code and created on first upload (`CreateIfNotExistsAsync`, `PublicAccessType.None`). An upload overwrites (`overwrite: true`). A missing blob or container is `None`.
- **The connection**, `src/Modules/Tickets/Tickets.Infrastructure/Storage/BlobServiceClients.cs`: Aspire's connection `blobs` comes in two forms, and the client takes either. Locally it is Azurite's connection string; deployed it is the account's blob endpoint, a URI, reached with `DefaultAzureCredential` (the apps' managed identity), never a key. `.../Wiring/TicketsStorage.cs` registers the client and the store as singletons. `Azure.Storage.Blobs` and `Azure.Identity` are referenced only by `Tickets.Infrastructure.csproj`; no architecture rule holds them there.
- **The names**, `src/Modules/Tickets/Tickets.Application/AttachmentBlobs.cs`: `{ticket}/{attachment}`, two Guid v7 values the module generates, never a file name a user typed. The file name is kept on the ticket's row.
- **Writing**, `.../Commands/RaiseTicket/RaiseTicketHandler.cs`: the `Ticket` aggregate checks every rule first (at most 5 files, each at most 10 MB, `Ticket.MaximumAttachments`, `Attachment.MaximumSize`), then each file is uploaded, then the ticket is saved. A ticket is never saved without its files; a save that fails after the uploads leaves those blobs behind, unreferenced.
- **Reading**, `.../Queries/DownloadAttachment/DownloadAttachmentHandler.cs`: it reads the ticket through the tenant's rows first and opens the blob only for an attachment that ticket names, so another tenant's attachment is `404`. The endpoint answers every file as `application/octet-stream` with `Content-Disposition: attachment`, so an uploaded HTML file never renders in the portal's origin (PR #24).
- **The AppHost**, `src/AppHost/AppHost.cs`: `AddAzureStorage("storage").RunAsEmulator(azurite => azurite.WithDataVolume()).AddBlobs("blobs")`, referenced by the host with `.WithReference(blobs).WaitFor(blobs)`.
- **Deployed** (`infra/resources.bicep`, `storage` and `blobsForHost`; docs/adr/0058): a `StorageV2` account with `allowSharedKeyAccess: false` and `allowBlobPublicAccess: false`, Storage Blob Data Contributor for the apps' one managed identity, `ConnectionStrings__blobs` set to the account's blob endpoint, and since #65 (PR #87) a private endpoint only, with public network access disabled (docs/adr/0055).
- **Tests**:
  - `tests/Modules/Tickets.Tests/InMemoryBlobStore.cs` stands in for the store in the handlers' tests; `Storage/BlobServiceClientsTests.cs` checks that each form of the connection makes a client for the right endpoint.
  - Against the AppHost, `tests/Integration.Tests/TicketAttachmentsTests.cs`: two attachments are listed and download unchanged; a file over the limit is refused with its code; a ticket with the most attachments of the largest size is raised; `Another_tenants_ticket_and_its_attachments_are_not_found`.
  - `tests/Integration.Tests/BlobStorageTests.cs` reads the application model: Azurite keeps its files in a volume, as the database keeps its rows.
  - `web/app/e2e/raise-ticket.spec.ts` raises a ticket with two files in the browser and checks both downloads come back unchanged.

## Why

- **Azurite locally, Azure Blob Storage deployed:** issue #6 and the owner's decision on #10; Aspire's Azure Storage integration gives both from one resource.
- **The interface in Application, the SDK in Infrastructure:** so a blob storage pack replaces one seam (PR #24, citing software-fabric#602).
- **Files through the host**, not signed URLs to the browser: the reason is not recorded.
- **A volume for Azurite** (commit 46f4b59): a review finding on PR #24. PostgreSQL kept its rows in a volume and Azurite did not, so a restarted AppHost listed attachments whose download was a `404`.
- **No account key when deployed:** the storage account disables shared keys and the host reaches it with its identity (PR #64). Why keyless was chosen over a key in Key Vault is not recorded.
- **Overwrite on upload:** not recorded. Names are generated per attachment, so nothing writes the same name twice today.
- **The limits** (10 MB per file, 5 per ticket): PR #24's assumptions; the issue set no number.

## What it taught

- **Isolation rested on lookup order.** The blob names carry no tenant. A file is safe only because every path reads the tenant-filtered ticket before it opens a blob; `IBlobStore` itself opens any name it is given. Nothing fails if a later path skips that lookup. The owner's decision B2 on software-fabric#602 makes the tenant part of the name, read by the store from `ITenantContext`, never from its caller (software-fabric ADR-0020), and #89 brings the pilot to that rule.
- **Local state needs the same durability as the database.** Without the volume, Azurite and PostgreSQL disagreed after every restart. A blob store beside a database must keep its files as long as the database keeps the rows that name them.
- **Upload before save leaves orphans.** A failed save after the uploads leaves blobs nothing names. PR #24 accepted that and documented it on `RaiseTicketHandler`; nothing cleans them up.
- **The keyless path was first seen in code only.** PR #24 exercised Azurite alone; the endpoint-URI-and-identity branch was unit-tested for client construction until `infra/` existed (#62). The client is made by hand from the two forms of Aspire's `blobs` connection, not by Aspire's client integration for Blob Storage; why is not recorded.
- **One container named in code is enough for one module.** A second module storing files would need its own container, or a rule that keeps it out of this one; software-fabric ADR-0020 settles a container per module for the pack.

## Consequences

- #89 changes every blob name to start with its tenant, taken from `ITenantContext` inside the store, and moves attachments stored under the old names. Until it merges, this ADR describes `main`.
- This is fabric-pilot's half of the reference for the `backend.blob-storage: azure-blob` pack (software-fabric ADR-0020, B1); PharmaTraining is the other. The pack differs from this code where ADR-0020 decided so: create-only writes (O5, against this store's overwrite), the SDK confined to a building block (O4, against this module's Infrastructure), and a container per module. Once the pack lands, the pilot moves to it with `fabric change --set backend.blob-storage=azure-blob` (ADR-0020, X1) in a PR of its own.
