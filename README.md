# NexusMods.App.SingleProcess

## About

This library allows an application to be run as a single process, with multiple windows (and fully multithreaded) while
still allowing rich communication between the main process and the app's CLI.

## Motivation

It became clear that the Nexus Mods App would benefit significantly from being a single process application, issues such
as synchronisation of data between processes and message lag between windows were becoming a problem. This library attempts
to solve these issues by providing a framework for a single process application, where the UI is run through a "Main" process
and communication with the CLI is done through child processes connecting over IPC.

Non-goals of this library include multiple implementations of the IPC system (everything uses localhost tcp for simplicity),
and multiple CLI rendering/interaction techs. In this app the CLI is abstracted away and a default rendering implementation
is provided for Spectre. This is where the "opinionated" part of the library comes in, it is designed to be used for apps
structured in a similar way to the Nexus Mods App.

## Docs

More detailed documentation can be found [here](./docs/index.md)

## License

See [LICENSE.md](./LICENSE.md)
