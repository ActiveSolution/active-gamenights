module Backend.CompositionRoot

open Backend
open Giraffe

let config = Configuration.config.Value

type BackendEnv() =
    interface Storage.IStorage with member _.Tables = Storage.live config.ConnectionString
    interface ITemplateBuilder with
        member _.Templates =
            let settings =
                { new ITemplateSettings with
                    member _.BasePath = config.BasePath
                    member _.Domain = config.Domain }
            { new ITemplates with
                member _.Fragment(content) = Api.Shared.HtmlViews.fragment settings content 
                member _.FullPage(content) = Api.Shared.HtmlViews.fullPage settings content }
let env = BackendEnv()

module Api =
    let userController : HttpHandler = Api.User.controller env
    let confirmedGameNightController : HttpHandler = Api.ConfirmedGameNight.controller env
    let proposedGameNightController : HttpHandler = Api.ProposedGameNight.controller env
    let addGameInputFragment : HttpHandler = Api.ProposedGameNight.addGameInputFragment env
    let addDateInputFragment : HttpHandler = Api.ProposedGameNight.addDateInputFragment env
    let gameNightController : HttpHandler = Api.GameNight.controller env
    let navbarPage : HttpHandler = Api.Navbar.handler env
    let versionPage : HttpHandler = Api.Version.handler env

