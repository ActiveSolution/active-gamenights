module WebTests.Pages.LoginPage

open canopy.classic
open WebTests.Extensions

let _loginUrl rootUrl = rootUrl + "/user/add"

let _usernameInput = name "username"
let _submitButton = name "submit"
let _logoutDropdown = css "#logout-dropdown"
let _logoutButton = css "#logout-button"
let _loggedInUsername = css "#username"

let login rootUrl username =
    describe "Logging in"
    url (_loginUrl rootUrl)
    _usernameInput << username
    click _submitButton
    
let logout() =
    describe "Logging out"
    match someElement _loggedInUsername with
    | Some _ ->
        hover _logoutDropdown
        click _logoutButton
    | None ->
        () // already logged out

