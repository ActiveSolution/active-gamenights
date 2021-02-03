import * as Turbo from "@hotwired/turbo"

import LoadingButtonController from "./controllers/loading_button_controller"

import { Application } from "stimulus"
const application = Application.start()
application.register("loading-button", LoadingButtonController)
