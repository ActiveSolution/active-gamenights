import { ApplicationController } from 'stimulus-use'

export default class extends ApplicationController {
    static targets = [ "count" ]
    
    refresh() {
        fetch("/fragments/navbar/unvotedcount")
            .then(res => res.text())
            .then(html => {
                if (this.hasCountTarget) {
                    this.countTarget.innerHTML = html;
                }
            });
    }
}