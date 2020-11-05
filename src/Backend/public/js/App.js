let fetch = (method, endpoint, payload) => {
    let xhr = new XMLHttpRequest();
    xhr.open(method, endpoint, true);
    xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
    xhr.setRequestHeader('Content-Type', 'application/json');
    xhr.onload = function() {
        if (xhr.status == 200) {
            Turbolinks.clearCache();
            Turbolinks.visit(xhr.responseText);
        }
    };
    xhr.send(JSON.stringify(payload));
};
let logOut = target => {
    let username = target.dataset.username;
    fetch('DELETE',`/user/${username}`)
    return false;
};
let createGameNight = target => {
    let game1 = document.getElementById('create-game-night-game1').value;
    let game2 = document.getElementById('create-game-night-game2').value;
    let game3 = document.getElementById('create-game-night-game3').value;
    let date1 = document.getElementById('create-game-night-date1').value;
    let date2 = document.getElementById('create-game-night-date2').value;
    let date3 = document.getElementById('create-game-night-date3').value;
    let games = [];
    if (game1) games.push(game1);
    if (game2) games.push(game2);
    if (game3) games.push(game3);
    let dates = [];
    if (date1) dates.push(date1);
    if (date2) dates.push(date2);
    if (date3) dates.push(date3);

    let payload = { 'games': games, 'dates': dates }
    fetch('POST', "/gamenight", payload)
    return false;
};
let addGameVote = target => {
    let gameName = target.dataset.game;
    let gameNightId = target.dataset.gamenight;
    fetch('POST', `/gamenight/${gameNightId}/game/${gameName}/vote`);
    return false;
};

let removeGameVote = target => {
    let username = target.dataset.username;
    let gameName = target.dataset.game;
    let gameNightId = target.dataset.gamenight;
    fetch('DELETE', `/gamenight/${gameNightId}/game/${gameName}/vote/${username}`);
    return false;
};

let addDateVote = target => {
    let date = target.dataset.date;
    let gameNightId = target.dataset.gamenight;
    fetch('POST', `/gamenight/${gameNightId}/date/${date}/vote`);
    return false;
};

let removeDateVote = target => {
    let username = target.dataset.username;
    let date = target.dataset.date;
    let gameNightId = target.dataset.gamenight;
    fetch('DELETE', `/gamenight/${gameNightId}/date/${date}/vote/${username}`);
    return false;
};

var scrollPositionY = null;

let saveScrollPosition = () =>
{
    scrollPositionY = window.scrollY;
};

let recallScrollPosition = () =>
{
    if (scrollPositionY) {
        window.scrollTo(0, scrollPositionY);
    }
    scrollPositionY = null;
};

let toggleNavbarMenu = (target) => {
    document.getElementById("agn-navbar-burger").classList.toggle("is-active");
    document.getElementById("agn-navbar-menu").classList.toggle("is-active");
    return false;
};

let closeNavbarMenu = () => {
    document.getElementById("agn-navbar-burger").classList.remove("is-active");
    document.getElementById("agn-navbar-menu").classList.remove("is-active");
};

document.addEventListener('click', (event) => {
    let target = event.target;

    if (target.closest('#logout-button'))
    {
        closeNavbarMenu();
        return logOut(target);
    }

    switch(target.id) {
        case 'create-game-night-button':
            return createGameNight(target);
        case 'agn-navbar-burger':
            return toggleNavbarMenu(target);
        default:
            break;
    }
    if (target.classList.contains('add-game-vote-button')) { 
        return addGameVote(target);
    }
    if (target.classList.contains('remove-game-vote-button')) {
        return removeGameVote(target);
    }
    if (target.classList.contains('add-date-vote-button')) {
        return addDateVote(target);
    }
    if (target.classList.contains('remove-date-vote-button')) {
        return removeDateVote(target);
    }
});


document.addEventListener("turbolinks:before-visit", (event) => {
    saveScrollPosition();
});
document.addEventListener("turbolinks:render", (event) => {
    recallScrollPosition();
});
