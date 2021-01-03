namespace Backend.Api.Shared

open FsHotWire.Feliz
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine


module Bulma =
    let fieldControl (elements : ReactElement list) =
        Bulma.field.div [
            Bulma.control.div elements 
        ]
        
    let fieldLabelControl (label: string) (elements : ReactElement list) =
        Bulma.field.div [
            Bulma.label label
            Bulma.control.div elements 
        ]
        
    let faIcon classes =
        Bulma.icon [
            prop.children [
                Html.i [
                    prop.classes classes 
                ]
            ]
        ]

    module Icons =
        let plusIcon = 
            faIcon [ "fas"; "fa-plus" ]
