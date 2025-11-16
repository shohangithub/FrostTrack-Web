export {};
declare global {
  interface Array<T> {
    insertThenClone(input: T): Array<T>;
    clone(): Array<T>;
    sum(): number;
  }

  interface Date {
    systemFormat(): string;
  }
}
