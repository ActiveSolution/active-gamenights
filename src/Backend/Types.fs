namespace Backend

open Domain
open System.Threading.Tasks


type BrowserError =
    | Duplicate 
    | NotFound of NotFoundError
    | Validation of ValidationError
    | Domain of DomainError
    | MissingUser of string
    
type ApiError =
    | Duplicate 
    | NotFound of NotFoundError
    | Validation of ValidationError
    | Domain of DomainError
    

// Web
type BasePath = BasePath of string
type Domain = Domain of string
type BrowserResponse =
    | Html of string
    | Redirect of string
type BrowserResult = Result<BrowserResponse, BrowserError>
type BrowserTaskResult = Task<BrowserResult>

type ApiResponse<'T> =
    | Json of 'T
    | Created of Location: string
    | Accepted
type ApiResult<'T> = Result<ApiResponse<'T>, ApiError>
type ApiTaskResult<'T> = Task<ApiResult<'T>>
