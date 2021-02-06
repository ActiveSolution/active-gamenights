import {Controller} from "stimulus"

export default class extends Controller {
  static targets = [ "count" ]

  fetchCount() {
    fetch("/fragments/navbar/unvotedcount").then(res => res.text()).then(html => {
      if (this.hasCountTarget) {
        this.countTarget.innerHTML = html;
      }
    });
  }

  initialize() {
    this.fetchCount();
  }


  delayedFetch() {
    setTimeout(() => this.fetchCount(), 250);
    setTimeout(() => this.fetchCount(), 500);
  }
}
