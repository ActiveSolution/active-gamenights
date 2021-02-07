import { ApplicationController } from 'stimulus-use'

export default class extends ApplicationController {
    
    connect() {
        if (!this.isPreview) {
            this.dispatch("connected")
        }
    }
}