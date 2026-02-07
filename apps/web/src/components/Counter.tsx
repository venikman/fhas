import React, { useState, useCallback } from 'react';

export interface CounterProps {
  initialValue?: number;
  step?: number;
  min?: number;
  max?: number;
  onChange?: (value: number) => void;
}

const Counter: React.FC<CounterProps> = ({
  initialValue = 0,
  step = 1,
  min = Number.MIN_SAFE_INTEGER,
  max = Number.MAX_SAFE_INTEGER,
  onChange,
}) => {
  const [count, setCount] = useState(initialValue);

  const updateCount = useCallback(
    (newValue: number) => {
      const clamped = Math.max(min, Math.min(max, newValue));
      setCount(clamped);
      onChange?.(clamped);
    },
    [min, max, onChange],
  );

  const increment = useCallback(() => {
    updateCount(count + step);
  }, [count, step, updateCount]);

  const decrement = useCallback(() => {
    updateCount(count - step);
  }, [count, step, updateCount]);

  const reset = useCallback(() => {
    updateCount(initialValue);
  }, [initialValue, updateCount]);

  return (
    <div
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '0.5rem',
        padding: '0.5rem',
        border: '1px solid #ccc',
        borderRadius: '4px',
      }}
    >
      <button
        type="button"
        onClick={decrement}
        disabled={count <= min}
        aria-label="Decrement"
      >
        −
      </button>
      <span
        data-testid="counter-value"
        style={{ minWidth: '3ch', textAlign: 'center' }}
      >
        {count}
      </span>
      <button
        type="button"
        onClick={increment}
        disabled={count >= max}
        aria-label="Increment"
      >
        +
      </button>
      <button type="button" onClick={reset} aria-label="Reset">
        Reset
      </button>
    </div>
  );
};

export default Counter;
