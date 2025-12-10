export {};
declare global {
  interface Array<T> {
    insertThenClone(input: T): Array<T>;
    clone(): Array<T>;
    sum(): number;
  }

  interface Date {
    /**
     * Formats date as YYYY-MM-DD for HTML date inputs (local timezone)
     */
    systemFormat(): string;

    /**
     * Converts date to UTC ISO string for API calls
     */
    toUtcIso(): string;
  }
}
