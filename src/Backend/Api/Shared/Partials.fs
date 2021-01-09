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
        
    let submitButton (text: string) =
        Bulma.field.div [
            Bulma.control.div [
                Bulma.button.button [
                    color.isPrimary
                    prop.type'.submit
                    prop.text text
                ]
            ]
        ]
        
    let submitButtonWithCancel (okText: string) (cancelText: string) cancelHref =
        Bulma.field.div [
            field.isGrouped
            prop.children [
                Bulma.control.div [
                    Bulma.button.button [
                        color.isPrimary
                        prop.type'.submit
                        prop.text okText
                    ]
                ]
                Bulma.control.div [
                    Bulma.button.a [
                        color.isLight
                        prop.href cancelHref
                        prop.text cancelText
                    ]
                ]
            ]
        ]

    module Icons =
        let plusIcon = 
            faIcon [ "fas"; "fa-plus" ]
