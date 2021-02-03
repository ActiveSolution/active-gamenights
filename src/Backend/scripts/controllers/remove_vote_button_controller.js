import { Controller } from "stimulus"

export default class extends Controller {
  static values = { "text": String }
  
  setActive() {
    this.element.classList.add("is-danger")
    this.element.classList.remove("is-info")
    this.element.innerHTML = `<s>${this.textValue}</s>`
  }
  
  setInactive() {
    this.element.classList.add("is-info")
    this.element.classList.remove("is-danger")
    this.element.innerHTML = this.textValue
  }
}