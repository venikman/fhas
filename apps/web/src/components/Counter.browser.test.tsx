import { render } from '@rstest/browser-react';
import Counter from './Counter';

const getValue = () =>
  document.querySelector('[data-testid="counter-value"]')?.textContent;

const getButtons = () => {
  const buttons = document.querySelectorAll('button');
  return {
    decrement: buttons[0] as HTMLButtonElement,
    increment: buttons[1] as HTMLButtonElement,
    reset: buttons[2] as HTMLButtonElement,
  };
};

/** Wait for DOM to reflect expected value after React state update */
const waitForValue = (expected: string, timeout = 1000): Promise<void> =>
  new Promise((resolve, reject) => {
    const start = Date.now();
    const check = () => {
      if (getValue() === expected) {
        resolve();
      } else if (Date.now() - start > timeout) {
        reject(new Error(`Expected "${expected}", got "${getValue()}"`));
      } else {
        requestAnimationFrame(check);
      }
    };
    check();
  });

test('renders with default initial value of 0', async () => {
  await render(<Counter />);
  expect(getValue()).toBe('0');
});

test('renders with custom initial value', async () => {
  await render(<Counter initialValue={10} />);
  expect(getValue()).toBe('10');
});

test('increments by 1 by default', async () => {
  await render(<Counter />);
  const { increment } = getButtons();

  increment.click();
  await waitForValue('1');

  increment.click();
  await waitForValue('2');
});

test('decrements by 1 by default', async () => {
  await render(<Counter initialValue={5} />);
  const { decrement } = getButtons();

  decrement.click();
  await waitForValue('4');
});

test('uses custom step value', async () => {
  await render(<Counter step={5} />);
  const { increment, decrement } = getButtons();

  increment.click();
  await waitForValue('5');

  decrement.click();
  await waitForValue('0');
});

test('respects min boundary', async () => {
  await render(<Counter initialValue={1} min={0} />);
  const { decrement } = getButtons();

  decrement.click();
  await waitForValue('0');

  // Should not go below min - button should be disabled
  expect(decrement.disabled).toBe(true);
});

test('respects max boundary', async () => {
  await render(<Counter initialValue={9} max={10} />);
  const { increment } = getButtons();

  increment.click();
  await waitForValue('10');

  // Should not go above max - button should be disabled
  expect(increment.disabled).toBe(true);
});

test('reset returns to initial value', async () => {
  await render(<Counter initialValue={5} />);
  const { increment, reset } = getButtons();

  increment.click();
  await waitForValue('6');

  increment.click();
  await waitForValue('7');

  reset.click();
  await waitForValue('5');
});

test('calls onChange with new value', async () => {
  const values: number[] = [];
  const onChange = (v: number) => values.push(v);

  await render(<Counter onChange={onChange} />);
  const { increment, decrement } = getButtons();

  increment.click();
  await waitForValue('1');

  increment.click();
  await waitForValue('2');

  decrement.click();
  await waitForValue('1');

  expect(values).toEqual([1, 2, 1]);
});
