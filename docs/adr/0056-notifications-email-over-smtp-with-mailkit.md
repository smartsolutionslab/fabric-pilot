# Notifications: e-mail over SMTP with MailKit, sent at most once per event

**Status:** Accepted. It records what the pilot built by hand for charter feature 4 (#8, PR #44; #66, PR #73; #74, PR #75), as the owner's decision X2 on software-fabric#602 (2026-11-21) asks (#90). A pilot decision, not generated. It informs software-fabric ADR-0019 (`docs/adr/0019-email.md`, `backend.email`).

## Context

When a ticket's status changes, the customer who raised it gets a notice in the portal and an e-mail (charter feature 4, #8). The catalog has no e-mail option, so the charter lists e-mail under "Hand-written here, not generated", as an M5 catalog candidate. The owner decided on #8 (2026-11-12): "E-mail goes out through MailKit behind one interface; locally to the Mailpit the AppHost already runs." Why MailKit rather than another library or a provider's own API is not recorded.

The Mailpit the AppHost already ran was the keycloak pack's, for Keycloak's own e-mail (docs/adr/fabric/backend-auth-keycloak.md): run mode only, the realm's `smtpServer` pointing at it.

## What was built

- **The interface**, in the module's Application project: `src/Modules/Notifications/Notifications.Application/IEmailSender.cs` (`SendAsync(EmailMessage, CancellationToken)`; a failure throws, so the caller's message is retried) and `EmailMessage.cs` (`To`, `Subject`, `Body`: one plain-text message to one address, from the configured sender).
- **The sender**, `src/Modules/Notifications/Notifications.Infrastructure/Email/MailKitEmailSender.cs`: one SMTP connection per send, a 30-second timeout per network operation, the caller's token bounding the whole send, and a sign-in only when credentials are configured. MailKit 5.0.0 (`Directory.Packages.props`), which brings `MimeKit.Core`. Only `Notifications.Infrastructure` references MailKit (`Notifications.Infrastructure.csproj`); no architecture rule holds it there.
- **The settings**, `.../Email/SmtpSettings.cs`, read once at startup:
  - the connection string `mail` is `smtp://host:port` (STARTTLS required) or `smtps://host:port` (TLS from the start); anything else is refused;
  - the sender is `Notifications:SenderAddress`;
  - a server without TLS is refused unless `Notifications:AllowUnencryptedSmtp` is true, which only the AppHost sets, for Mailpit;
  - `Notifications:SmtpUserName` and `Notifications:SmtpPassword` are given together or not at all; without them nothing signs in. `.../Email/SmtpCredentials.cs` names the user in its text and never shows the password.
- **The wiring**, `.../Wiring/NotificationsServices.cs`: `SmtpSettings` and `MailKitEmailSender` as singletons.
- **The AppHost**, `src/AppHost/AppHost.cs`: in run mode, the Mailpit container is declared before the run-mode block as a nullable `mailpit` (line 42) so the host's wiring can name it later. The host gets Mailpit's `smtp` endpoint as `ConnectionStrings__mail`, the sender `noreply@example.com`, `Notifications__AllowUnencryptedSmtp=true`, and waits for Mailpit. When publishing, the AppHost declares the parameter `smtp-user-name` and the secret parameter `smtp-password` and gives them to the host as `Notifications__SmtpUserName` and `Notifications__SmtpPassword`.
- **Deployed** (docs/adr/0058): Mailpit is not deployed. `SMTP_CONNECTION` and `NOTIFICATIONS_SENDER_ADDRESS` are `infra/main.bicep` parameters; an optional `SMTP_USER_NAME` and `SMTP_PASSWORD`, the password kept in Key Vault as `smtp-password` and created only when a user name is set (`infra/resources.bicep`, `smtpSignsIn`). `infra/check.sh` lists `Notifications__AllowUnencryptedSmtp` as the one host setting a deployment does not set.
- **Using it at most once per event**, `src/Modules/Notifications/Notifications.Application/Consumers/TicketStatusChangedV1Consumer.cs` with `Notifications.Domain/Notices/Notice.cs`:
  1. Inside `TenantScope.Begin(message.Metadata)`, the consumer finds the notice caused by `Metadata.EventIdentifier`, or creates it. A unique index on `causing_event` holds one notice per event (`.../Persistence/Configurations/NoticeConfiguration.cs`).
  2. A notice already e-mailed ends the delivery.
  3. Otherwise the delivery claims the e-mail (`Notice.TryClaimEmail`) and saves the claim over the row version it read (`xmin`, migration `20261113090336_ClaimNoticeEmails`). Of two copies handled at once, one claims and the other fails on the row version or on a live claim, throws, and is retried.
  4. It sends under a deadline: a send still running `Notice.EmailSendDeadline` (30 s) after the claim is cancelled, inside the claim's lease, `Notice.EmailClaimLease` (45 s). The lease is longer than the SMTP timeout and shorter than Wolverine's retries, about 63 s (PR #44), so a copy retried after a crash still sends.
  5. A failed or cancelled send gives the claim back and rethrows; Wolverine retries the message and then dead-letters it (`wolverine.wolverine_dead_letters`). A sent e-mail marks the notice e-mailed.
  - A second e-mail goes out only if the host dies between sending and marking the notice e-mailed.
- **Tests**:
  - `tests/Modules/Notifications.Tests/Email/MailKitEmailSenderTests.cs`, against a loopback server that offers no extensions and stops answering once it has the message (`Email/StallingSmtpServer.cs`): a server without STARTTLS is refused when TLS is required, a server that does not speak TLS is refused when TLS comes first, and a cancelled send stops before the server accepts the message.
  - `Email/SmtpSettingsTests.cs`: the two schemes, unencrypted only when configured, credentials together or not at all, the password never in the text. `RecordingEmailSender.cs` stands in for the sender in the consumer's tests.
  - Against the AppHost: `tests/Integration.Tests/NotificationsTests.cs` delivers a status change twice and reads exactly one e-mail from Mailpit's API (`MailpitInbox.cs`); `NotificationsSmtpAuthenticationTests.cs` runs a second Mailpit that requires a sign-in (`smtp-authenticating` in `AspireFixture.cs`, `MP_SMTP_AUTH`): the configured user's e-mail arrives, and a wrong password fails the send and delivers nothing.

## Why

- **MailKit behind one interface, Mailpit locally:** the owner's decision on #8. The reason for MailKit over the alternatives is not recorded.
- **TLS required except for Mailpit:** the first review round of PR #44 blocked a sender that would send unencrypted outside Mailpit, which offers no TLS. The permission is a setting only the AppHost gives, in run mode.
- **The claim, the lease and the deadline:** #8 asked that the same event twice sends one e-mail. The first review round of PR #44 found a copy delivered while the first was still sending would send again; the claim saved over the row version closes that. The second round found a send could outlive its claim; the deadline closes that.
- **The sign-in** (#66, PR #73): found while writing the Azure deployment (#62). Azure Communication Services' SMTP and most providers require authentication, and the sender could only use an unauthenticated relay. The user name and password are two settings, not part of the `mail` URI, because a password in a URI's user-info would trip `NoSecretInSettings` and provider user names would need URL-escaping. They are declared only when publishing, so a local run does not prompt for a parameter Mailpit never uses (PR #73).
- **MailKit 5** (#74, PR #75): Dependabot's major bump broke the build, because `SmtpClient.SendAsync(MimeMessage, ...)` became an extension method; `using MailKit;` was the whole port. PR #75 lists MailKit 5's breaking changes and which touch this module.

## What it taught

- **The claim is domain code, not a library.** It is a lease and a deadline on the module's own aggregate, tuned against the SMTP timeout and Wolverine's retry schedule; generic "send once" plumbing would not know either. The generator came to the same conclusion (software-fabric ADR-0019, E3: no building block for it, an AGENTS.md rule to deduplicate on `Metadata.EventIdentifier` instead).
- **Credentials were missed until deployment.** A local catcher needs no sign-in, so nothing local showed that real providers do. The gap surfaced only when `infra/` was written (#62), and the sign-in followed as its own issue (#66).
- **Mailpit belonged to the wrong owner.** Keycloak's pack added Mailpit, so the notifications module depended on the identity provider's choice for its local catcher, and the AppHost had to hoist it into a nullable variable to reach it. software-fabric ADR-0019 E2 moved Mailpit into the base for this reason.
- **TLS has only been seen refusing.** Neither Mailpit in the tests offers TLS, so TLS is observed as refusing a server that lacks it, not as completing a handshake and delivering; nor has a real provider been seen taking mail from the deployed host (PR #75, "Not covered"; PR #73).
- **A refused sign-in looks like any other delivery failure**: the host logs `AuthenticationException`, retries for about a minute, then dead-letters it; the portal notice is still shown (`infra/README.md`, step 4). That is what "a failure throws" buys, and the runbook has to say where to look.

## Consequences

- This is the code the `backend.email: smtp` pack is designed from (software-fabric ADR-0019, E1). Once the pack lands, the pilot moves to it with `fabric change --set backend.email=smtp` (ADR-0019, X1; M5-03) and removes its hand-written sender in a PR of its own. The configuration keys' names may change then: the pilot's are its module's (`Notifications:*`).
- The at-most-once claim stays in this module either way (ADR-0019, E3).
