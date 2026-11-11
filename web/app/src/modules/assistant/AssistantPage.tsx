import { AgentPanel } from '@/agents/AgentPanel';

/** The module the agent panel choice brings; its screen is a conversation with its agent (docs/adr). */
export function AssistantPage() {
  return (
    <main>
      <h1>Assistant</h1>
      <AgentPanel agent="assistant" />
    </main>
  );
}
