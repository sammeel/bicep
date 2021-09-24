
module passthrough 'br:biceptestdf.azurecr.io/restore/passthrough:v1_2021-09-24T06-00-00.000Z_e18eb569-8f47-466f-ba5a-4a364cd85cee' = {
  name: 'passthrough'
  params: {
    text: 'hello'
    number: 42
  }
}

module storage 'br:biceptestdf.azurecr.io/restore/storage:v1_2021-09-24T06-00-00.000Z_e18eb569-8f47-466f-ba5a-4a364cd85cee' = {
  name: 'storage'
  params: {
    name: passthrough.outputs.result
  }
}

output blobEndpoint string = storage.outputs.blobEndpoint
    