module WebTests.Extensions

open OpenQA.Selenium
open canopy.classic

let name value = sprintf "[name = '%s']" value |> css
