module Entities

open Microsoft.WindowsAzure.Storage.Table

type LogEntity() = 
    inherit TableEntity()
    member val Role = "" with get, set
    member val RoleInstance = "" with get, set
    member val Level = 0 with get, set
    member val Message = "" with get, set
    member val Pid = 0 with get, set
    member val Tid = 0 with get, set
    member val EventId = 0 with get, set
