import * as Turbo from "@hotwired/turbo"

import CssClassController from "./controllers/css_class_controller"
import RemoveVoteButtonController from "./controllers/remove_vote_button_controller"
import ActivePageController from "./controllers/active_page_controller"
import UnvotedCountController from "./controllers/unvoted_count_controller"
import RefreshVoteCountController from "./controllers/refresh_vote_count_controller"

import { Application } from "stimulus"
const application = Application.start()
application.register("css-class", CssClassController)
application.register("remove-vote-button", RemoveVoteButtonController)
application.register("active-page", ActivePageController)
application.register("unvoted-count", UnvotedCountController)
application.register("refresh-vote-count", RefreshVoteCountController)
