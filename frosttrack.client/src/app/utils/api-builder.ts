export const getApiEndpoint = (params: any, api: string) => {
  const paramsDictionary = checkParams(params);
  return createApiEndpoint(api, paramsDictionary);
};

const createApiEndpoint = (api: string, paramsDictionary: any) => {
  let apiEndpoint = api;
  let isSetOption = false;
  for (let key in paramsDictionary) {
    if (!isSetOption) {
      apiEndpoint = apiEndpoint + '?';
      isSetOption = true;
    }
    apiEndpoint = apiEndpoint.concat(key + '=' + paramsDictionary[key], '&');
  }
  apiEndpoint = apiEndpoint.slice(0, apiEndpoint.length - 1);
  return apiEndpoint;
};

const checkParams = (params: any) => {
  let paramsDictionary: any = {};
  for (let item in params) {
    if (params[item] != undefined && params[item] !== '') {
      paramsDictionary[item] = params[item];
    }
  }
  return paramsDictionary;
};
