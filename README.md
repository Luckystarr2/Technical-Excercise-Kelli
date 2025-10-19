# Technical-Excercise-Kelli

Clone repository and run the solution in Technical-Excercise

From there you can run the publish profile in Technical-Excercise: KGS_Demo.Local.publish.publish.xml
-This will create the database along with the procedures

You can then run the CounterpointConnector project which will open swagger to test the api
-/api/tickets
You can use this json for the input:
{
  "customerAccountNo": "CUST1001",
  "lines": [
    { "sku": "SKU-100", "qty": 2 },
    { "sku": "SKU-200", "qty": 1, "overridePrice": 19.99 }
  ]
}
-Expected results should be:
{
  "ticketID": 10005,
  "subtotal": 39.97,
  "taxAmount": 3.3,
  "total": 43.27
}

You can run the CounterpointConnector.Tests by going to Test -> Run All Tests
-2 Tests should pass

Finally you can run the ETL project KGS.Demo.ETL
-The CSV file has been provided
-Expected result should be:
Validated 3 records.
Upserted 3 records into dbo.PayrollEligibility.
Done.
