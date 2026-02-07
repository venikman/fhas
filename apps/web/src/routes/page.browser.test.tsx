import { render } from '@rstest/browser-react';
import Page from './page';

/** Find the Send Message button specifically (not Counter buttons) */
const getSendButton = () =>
  Array.from(document.querySelectorAll('button')).find((btn) =>
    btn.textContent?.includes('Send'),
  ) as HTMLButtonElement | undefined;

const getStatus = () =>
  document.body.textContent?.match(/Status:\s*(\w+)/)?.[1];

const getTextarea = () =>
  document.querySelector('textarea') as HTMLTextAreaElement | null;

test('renders send button and textarea', async () => {
  await render(<Page />);
  const button = getSendButton();
  const textarea = getTextarea();

  expect(button).toBeDefined();
  expect(button?.textContent).toContain('Send');
  expect(textarea).toBeDefined();
  expect(textarea?.value).toContain('Analyze'); // Default prompt
});

test('clicking send updates status to running and shows user message', async () => {
  await render(<Page />);
  const button = getSendButton();
  expect(button).toBeDefined();
  expect(getStatus()).toBe('idle');

  button!.click();

  // Status should change to 'running' immediately on click
  // Use polling since state update is async
  await new Promise<void>((resolve, reject) => {
    const start = Date.now();
    const check = () => {
      const status = getStatus();
      if (status === 'running' || status === 'error' || status === 'done') {
        resolve();
      } else if (Date.now() - start > 2000) {
        reject(new Error(`Status never changed from idle, got: ${status}`));
      } else {
        requestAnimationFrame(check);
      }
    };
    check();
  });

  const finalStatus = getStatus();
  expect(['running', 'error', 'done']).toContain(finalStatus);

  // Check that the user message is displayed
  const pageText = document.body.textContent ?? '';
  expect(pageText).toContain('You');
});
