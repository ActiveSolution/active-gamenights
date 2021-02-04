import * as Turbo from "@hotwired/turbo"

import CssClassController from "./controllers/css_class_controller"
import RemoveVoteButtonController from "./controllers/remove_vote_button_controller"

import { Application } from "stimulus"
const application = Application.start()
application.register("css-class", CssClassController)
application.register("remove-vote-button", RemoveVoteButtonController)
