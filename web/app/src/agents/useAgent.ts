import { randomUUID, type Message } from '@ag-ui/client';
import { useCallback, useEffect, useRef, useState } from 'react';
import { connectAgent } from './connectAgent';

/**
 * A conversation with one agent: its messages, updated as each answer streams in, whether a run is under way, and why
 * the last run failed. The agent keeps the conversation, so every run sends all of it. `state`, when given, is read as
 * each run starts and sent as the run's AG-UI state: what the page shows, which the agent's query reads as untrusted
 * context.
 */
export function useAgent(name: string, state?: () => unknown) {
  const [agent] = useState(() => connectAgent(name));
  const [messages, setMessages] = useState<readonly Message[]>([]);
  const [running, setRunning] = useState(false);
  const [failure, setFailure] = useState<string>();
  // The getter of the latest render, so a run sends what the page shows when it starts.
  const latestState = useRef(state);
  useEffect(() => {
    latestState.current = state;
  });

  useEffect(() => {
    const subscription = agent.subscribe({
      onMessagesChanged: ({ messages: changed }) => {
        setMessages([...changed]);
      },
      // A failure the host streams as RUN_ERROR ends the run without rejecting runAgent.
      onRunErrorEvent: ({ event }) => {
        setFailure(event.message);
      },
    });
    return () => {
      subscription.unsubscribe();
      agent.abortRun();
    };
  }, [agent]);

  const send = useCallback(
    async (text: string) => {
      setFailure(undefined);
      setRunning(true);
      const read = latestState.current;
      if (read) {
        agent.setState(read());
      }
      agent.addMessage({ id: randomUUID(), role: 'user', content: text });
      try {
        await agent.runAgent();
      } catch (error) {
        setFailure(error instanceof Error ? error.message : String(error));
      } finally {
        setRunning(false);
      }
    },
    [agent],
  );

  return { messages, running, failure, send };
}
