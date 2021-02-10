import * as Turbo from "@hotwired/turbo"
import Flatpickr from 'stimulus-flatpickr'
require("flatpickr/dist/flatpickr.css")
require("./main.css")


import CssClassController from "./controllers/css_class_controller"
import RemoveVoteButtonController from "./controllers/remove_vote_button_controller"
import UnvotedCountController from "./controllers/unvoted_count_controller"
import RefreshVoteCountController from "./controllers/refresh_vote_count_controller"
import TurboStreamController from "./controllers/turbo_stream_controller"

import { Application } from "stimulus"
const application = Application.start()
application.register('flatpickr', Flatpickr)
application.register("css-class", CssClassController)
application.register("remove-vote-button", RemoveVoteButtonController)
application.register("unvoted-count", UnvotedCountController)
application.register("refresh-vote-count", RefreshVoteCountController)
application.register("turbo-stream", TurboStreamController)
