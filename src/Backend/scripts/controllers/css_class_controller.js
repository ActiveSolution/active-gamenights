import {Controller} from "stimulus"

export default class extends Controller {
    static classes = ["name"]
    static targets = ["element"]

    addClass() {
        if (this.hasElementTarget) {
            this.elementTargets.forEach(e => e.classList.add(this.nameClass));
        } else {
            this.element.classList.add(this.nameClass);
        }
    }

    toggleClass() {
        if (this.hasElementTarget) {
            this.elementTargets.forEach(e => e.classList.toggle(this.nameClass));
        } else {
            this.element.classList.toggle(this.nameClass)
        }
    }
}