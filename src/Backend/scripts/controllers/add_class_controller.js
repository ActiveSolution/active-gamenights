import { Controller } from "stimulus"

export default class extends Controller {
  static classes = [ "name" ]
  
  addClass() {
    this.element.classList.add(this.nameClass)
  }
}