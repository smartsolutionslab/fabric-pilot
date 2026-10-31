import { StrictMode, type ReactNode } from 'react';
import { createRoot } from 'react-dom/client';
import { withAuth } from './providers/auth';
import { withStore } from './providers/store';
import { withRouter } from './providers/router';
import { App } from './App';
import './index.css';

/** Wraps the app in one provider; listed outermost first. */
type Provider = (children: ReactNode) => ReactNode;

const providers: Provider[] = [
  withAuth,
  withStore,
  withRouter
];

const root = document.getElementById('root');
if (!root) {
  throw new Error('index.html has no #root element.');
}

createRoot(root).render(
  <StrictMode>{providers.reduceRight<ReactNode>((children, provide) => provide(children), <App />)}</StrictMode>,
);
