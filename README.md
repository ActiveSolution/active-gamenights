## Building the project

This project uses a [FAKE](https://fake.build) script to run build tasks (build.fsx). To build locally:
1. `dotnet tool restore` to restore the required tools (paket and fake-cli)
2. `dotnet fake build` to run the build

## Running the project

### Requirements
To be able to run the project some config settings are needed, see `Configuration.fs` and setup up the relevant config settings using [dotnet user-secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-5.0&tabs=windows)
The project uses azure table storage, either setup a storage account in Azure, or use a local emulator.


To run the project locally in watch mode:
`dotnet fake build --target WatchApp`



## Creating a release
The project uses github actions for ci and deployment. To create a new production release a new version entry must be added in CHANGELOG.md.

The github actions for CI and deployment are run using FAKE tasks as well, see `build.fsx` and `.github/workflows.ci.yaml`, and `.github/workflows.deploy.yaml`.

