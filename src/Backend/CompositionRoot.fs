module Backend.CompositionRoot

open Backend
open Giraffe
open Infrastructure

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
                member _.Fragment(content) = Api.Shared.HtmlViews.fragment content 
                member _.FullPage user unvotedCount page content = Api.Shared.HtmlViews.fullPage settings user page unvotedCount content }
let env = BackendEnv()

module Api =
    let userController : HttpHandler = Api.User.controller env
    let proposedGameNightController : HttpHandler = Api.ProposedGameNight.controller env
    let gameNightController : HttpHandler = Api.GameNight.controller env
    let gameController : HttpHandler = Api.Game.controller env
    let versionPage : HttpHandler = Api.Version.handler env
    
    module Fragments =
        module ProposedGameNight =
            let gameSelect : HttpHandler = Api.ProposedGameNight.Fragments.gameSelectFragment env
            let addGameSelect : HttpHandler = Api.ProposedGameNight.Fragments.addGameSelectFragment env
            let addDateInput : HttpHandler = Api.ProposedGameNight.Fragments.addDateInputFragment 
            let addGameNightLink : HttpHandler = Api.ProposedGameNight.Fragments.addGameNightLinkFragment env
        module Game =
            let addGameLink : HttpHandler = Api.Game.Fragments.addGameLinkFragment env
        module Navbar =
            let unvotedCount : HttpHandler = Api.Navbar.unvotedCountFragment env
