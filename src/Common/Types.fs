[<AutoOpen>]
module Common.Types

type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>>
