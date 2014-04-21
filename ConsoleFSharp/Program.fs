module Main

open System
open System.Text
open System.Threading
open System.Configuration
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table
open RestSharp

//TODO: find a non-mutable way of doing this
let mutable _lastQuery = DateTimeOffset.UtcNow

let GetLogsTable = 
    let cs = ConfigurationManager.AppSettings.["StorageConnectionString"]
    let storageAccount = CloudStorageAccount.Parse(cs)
    let tableClient = storageAccount.CreateCloudTableClient()
    let table = tableClient.GetTableReference("WADLogsTable")
    table

let GetLogs(table : CloudTable) = 
    let query = 
        TableQuery<Entities.LogEntity>()
            .Where(TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan, _lastQuery))
    let sb = StringBuilder()
    for entry in table.ExecuteQuery(query) do
        ignore (sb.Append(entry.Message).Append(", ").Append(entry.Timestamp).AppendLine())
        _lastQuery <- entry.Timestamp
    sb.ToString()

//TODO: gzip https://service.sumologic.com/ui/help/Default.htm#cshid=4036
let StreamToUrl(content : string) = 
    let client = new RestClient()
    client.BaseUrl <- "https://collectors.sumologic.com"
    let request = new RestRequest()
    request.Resource <- ConfigurationManager.AppSettings.["SumoLogicResource"]
    request.Method <- Method.POST
    ignore (request.AddFile("someName", Encoding.UTF8.GetBytes(content), "someFileName"))
    ignore (client.ExecuteAsync(request, 
                                fun (response : IRestResponse) -> 
                                    printf "Response status: %A" response.ResponseStatus
                                    printf "Response status code: %A" response.StatusCode))

let isEmptyMessage message = (String.IsNullOrWhiteSpace(message))
let sleep() = Thread.Sleep(TimeSpan.FromMinutes(1.0))

[<EntryPoint>]
let main argv = 
    let table = GetLogsTable
    while (true) do
        let message = GetLogs table
        match message with
        | _ when isEmptyMessage message -> sleep()
        | x -> StreamToUrl x        
    0 // return an integer exit code
