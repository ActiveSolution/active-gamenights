import * as Turbo from "@hotwired/turbo"

import AddClassController from "./controllers/add_class_controller"
import RemoveVoteButtonController from "./controllers/remove_vote_button_controller"

import { Application } from "stimulus"
const application = Application.start()
application.register("add-class", AddClassController)
application.register("remove-vote-button", RemoveVoteButtonController)
