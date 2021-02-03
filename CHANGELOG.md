# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
* Slack notification
* Export Calendar event
* Edit Game (BGG-link, image, notes, number of players)
* Add filters for GET /api/gamenight (e.g. ?status=completed/confirmed/cancelled/proposed&fromDate=2020-12-13)

## [0.4.0] - 2021-02-01
### Added
* Use Hotwire

## [0.3.5] - 2020-12-16
### Changed
* Change due-date to tomorrow.
* Restructure dependencies

## [0.3.4] - 2020-12-03
### Added
* Game nights are now confirmed 2 days prior the earliest suggested date
### Changed
* Show Confirmed and Proposed Game nights in the game night view.

## [0.3.3] - 2020-12-02
### Changed
* Run on linux
### Added
* Functions project

## [0.2.3] - 2020-11-10
### Added
* Api endpoints

## [0.2.2] - 2020-11-07
### Added
* Plausible analytics

## [0.2.1] - 2020-11-05
### Added
* Version endpoint
* FAKE build script

## [0.2.0] - 2020-11-05
### Added
* GitHub actions (build / deploy)
* Github link in navbar

## [0.1.0] - 2020-11-05
### Added
* Create user
* Add game night
* Voting