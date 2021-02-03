import { Controller } from "stimulus"

export default class extends Controller {
  static classes = [ "loading" ]

  setLoading() {
    this.element.classList.add(this.loadingClass)
  }
}