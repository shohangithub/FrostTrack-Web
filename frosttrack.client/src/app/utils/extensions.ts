export {}; // this file needs to be a module
Array.prototype.insertThenClone = function insertThenClone<T>(
  entity: T
): Array<T> {
  let _self = this;
  if (entity == null) return _self;
  const tempData: any = JSON.parse(JSON.stringify(_self));
  tempData.push(entity);
  _self = tempData;
  return _self;
};

Array.prototype.clone = function clone<T>(): Array<T> {
  debugger;
  let _self = this;
  const tempData: any = JSON.parse(JSON.stringify(_self));
  _self.length = 0;
  _self = tempData;
  return _self;
};

Array.prototype.sum = function (): number {
  return this.reduce(
    (accumulator, currentValue) => accumulator + currentValue,
    0
  );
};

Date.prototype.systemFormat = function (): string {
  // Returns YYYY-MM-DD format for date inputs (local timezone)
  // This is used for HTML date inputs which expect local date format
  const day = this.getDate().toString().padStart(2, '0');
  const month = (this.getMonth() + 1).toString().padStart(2, '0');
  const year = this.getFullYear();
  return `${year}-${month}-${day}`;
};

/**
 * Extension: Converts this Date to UTC ISO string for API calls
 * @returns ISO 8601 UTC string (e.g., "2025-01-15T10:30:00.000Z")
 */
Date.prototype.toUtcIso = function (): string {
  return this.toISOString();
};
