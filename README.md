# RemoteDirSync (Work in Progress)

RemoteDirSync is a two-part .NET 9 solution for inspecting and synchronizing directories between a desktop client and a remote agent:

- `RemoteDirSync.Bot` – ASP.NET Core Web API that runs on the target machine and exposes directory scan and file transfer endpoints.
- `RemoteDirSync.Desktop` – Avalonia-based cross-platform desktop UI for configuring connections, scanning remote directories, and initiating file transfers.

> **Status:** This project is a work in progress. **Use at your own risk.**  **I am not responsible for lost or overwritten files**
> APIs, data formats, and UI behavior may change without notice.

---

## Features (Current)

- Scan remote directories and retrieve structured results.
- Visual tree view of remote file system (folders and files).
- Compare and select items to transfer between two remote endpoints.
- Background job queue on the bot with simple status reporting.
- Basic file upload endpoint (`ReceiveFile`) for pushing files to the bot.

Planned and existing features are experimental; expect breaking changes.

---

## Projects

- `RemoteDirSync.Bot`
  - ASP.NET Core Web API.
  - Hosts endpoints such as `DirScan/ScanDir`, `DirScan/GetScanDirStatus`, `DirScan/SendFiles`, `DirScan/ReceiveFile`, and `DirScan/GetAllJobs`.
  - Uses a background job queue (`IBackgroundJobQueue`, `BackgroundJobWorker`) to run scan and file transfer jobs.
  - By default listens on `http://0.0.0.0:5000`.

- `RemoteDirSync.Desktop`
  - Avalonia UI app targeting .NET 9.
  - MVVM architecture (`ViewModels`, `Views`, `Models`).
  - Allows configuring remote connections, running scans, and initiating file moves between two remote states.

---

## Getting Started (Development)

### Prerequisites

- .NET 9 SDK
- A modern IDE or editor (Visual Studio, Rider, VS Code with C#)
- (Optional) Git for cloning

### Clone
git clone https://github.com/Gigagranddev/RemoteDirSync.git cd RemoteDirSync

### Running the Bot

From `RemoteDirSync.Bot`:
dotnet run

By default, the bot listens on:

- `http://0.0.0.0:5000`

You can check basic health at:

- `http://<host>:5000/Health`

### Running the Desktop App

From `RemoteDirSync.Desktop`:
dotnet run


In the UI, configure a connection to point at the machine and port where `RemoteDirSync.Bot` is running.

---

## Important Notes

- **Security:** The current setup is for development/testing. Authentication, authorization, and robust certificate handling are **not** implemented. Do not expose this service to untrusted networks.
- **Data Loss Risk:** File move/copy operations can overwrite data. Review paths and behavior carefully before using on important data.
- **API Stability:** Endpoints, models, and behavior are subject to change while the project is under active development.

---

## Contributing

This project is experimental and evolving. If you are interested in contributing:

1. Open an issue describing the bug or feature.
2. Fork the repo and create a feature branch.
3. Submit a pull request with a clear description of the change.

---

## License

This project does not currently declare a formal license in this README. Unless a license file is added, treat this as closed-source for personal evaluation and development only.

**Again: This is a work in progress. Use at your own risk. It is not my fault if you overwrite files that you don't want to**
