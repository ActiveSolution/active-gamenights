[<AutoOpen>]
module Infrastructure.Types

type AsyncResult<'TResult, 'TError> = Async<Result<'TResult, 'TError>>
