export { }; // this file needs to be a module
Array.prototype.insertThenClone = function insertThenClone<T>(entity: T): Array<T> {
  let _self = this;
  if (entity == null) return _self;
  const tempData: any = JSON.parse(JSON.stringify(_self));
  tempData.push(entity)
  _self = tempData;
  return _self;
}

Array.prototype.clone = function clone<T>(): Array<T> {
  debugger
  let _self = this;
  const tempData: any = JSON.parse(JSON.stringify(_self));
  _self.length = 0;
  _self = tempData;
  return _self;
}


Array.prototype.sum = function (): number {
  return this.reduce((accumulator, currentValue) => accumulator + currentValue, 0);
}

Date.prototype.systemFormat = function (): string {
  const day = this.getDate().toString().padStart(2, '0');
  const month = (this.getMonth() + 1).toString().padStart(2, '0');
  const year = this.getFullYear();
  //return `${month}/${day}/${year}`;
  return `${year}/${month}/${day}`;

}
