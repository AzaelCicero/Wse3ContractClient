# Wse3ContractClient
Consume old Web Services Enhancements (WSE) 3.0 SOAP12 based web services in modern way.

Usage example:
```
var factory = new Wse3GenericClientFactory();

var client = factory.Create<IWebServiceContract>("http//localhost/webservice.asmx", "webservice.namespace", "username", "pass", "machine");

var result = client.WebMethod(argument);
--OR--
var resultAsync = await client.WebMethodAsync(argument);

```