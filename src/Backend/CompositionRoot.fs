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
                member _.FullPage user content = Api.Shared.HtmlViews.fullPage settings user content }
let env = BackendEnv()

module Api =
    let userController : HttpHandler = Api.User.controller env
    let confirmedGameNightController : HttpHandler = Api.ConfirmedGameNight.controller env
    let proposedGameNightController : HttpHandler = Api.ProposedGameNight.controller env
    let gameNightController : HttpHandler = Api.GameNight.controller env
    let gameController : HttpHandler = Api.Game.controller env
    let versionPage : HttpHandler = Api.Version.handler env
    
    module Fragments =
        let gameSelect : HttpHandler = Api.ProposedGameNight.Fragments.gameSelectFragment env
        let addGameSelect : HttpHandler = Api.ProposedGameNight.Fragments.addGameSelectFragment env
        let addDateInput : HttpHandler = Api.ProposedGameNight.Fragments.addDateInputFragment env
        let addGameNightLink : HttpHandler = Api.ProposedGameNight.Fragments.addGameNightLinkFragment env
        let addGameLink : HttpHandler = Api.Game.Fragments.addGameLinkFragment env
            

